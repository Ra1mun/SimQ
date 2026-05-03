using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace SimQ.Client.Services;

/// <summary>
/// Thin HTTP client for the SimQ WebApi. All methods swallow network/HTTP
/// errors and return null/false so callers can treat "API offline" as a soft
/// state. The last error message is exposed via <see cref="LastError"/>.
/// </summary>
public sealed class SimQApiClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json;
    public ApiSettings Settings { get; }
    public string? LastError { get; private set; }

    public SimQApiClient(ApiSettings settings)
    {
        Settings = settings;
        _http = new HttpClient
        {
            BaseAddress = new Uri(settings.BaseUrl),
            Timeout = settings.RequestTimeout,
        };
        _json = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
    }

    public void Dispose() => _http.Dispose();

    // ---------- Health ----------

    /// <summary>
    /// Pings the WebApi liveness endpoint. Returns true only if the API
    /// responds with a successful status.
    /// </summary>
    public async Task<bool> CheckHealthAsync(CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(3));
            using var resp = await _http.GetAsync("healthz", cts.Token).ConfigureAwait(false);
            LastError = resp.IsSuccessStatusCode ? null : $"healthz {(int)resp.StatusCode}";
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return false;
        }
    }

    // ---------- Problems ----------

    public Task<ProblemListResponseDto?> GetProblemsAsync(CancellationToken ct = default)
        => GetAsync<ProblemListResponseDto>("Problems/v1/problems", ct);

    public Task<RegisterProblemResponse?> RegisterProblemAsync(RegisterProblemRequest request, CancellationToken ct = default)
        => PostAsync<RegisterProblemRequest, RegisterProblemResponse>("Problems/v1/problem", request, ct);

    public Task<bool> DeleteProblemAsync(string problemId, CancellationToken ct = default)
        => DeleteAsync($"Problems/v1/problem/{Uri.EscapeDataString(problemId)}", ct);

    // ---------- Tasks ----------

    public Task<SimulationTaskListResponseDto?> GetTasksAsync(CancellationToken ct = default)
        => GetAsync<SimulationTaskListResponseDto>("Tasks/tasks", ct);

    public Task<SimulationTaskDto?> GetTaskAsync(string taskId, CancellationToken ct = default)
        => GetAsync<SimulationTaskDto>($"Tasks/task/{Uri.EscapeDataString(taskId)}", ct);

    public Task<CreateTaskResponseDto?> CreateTaskAsync(CreateTaskRequestDto request, CancellationToken ct = default)
        => PostAsync<CreateTaskRequestDto, CreateTaskResponseDto>("Tasks/task", request, ct);

    public async Task<bool> StopTaskAsync(string taskId, CancellationToken ct = default)
    {
        try
        {
            using var resp = await _http.PostAsync($"Tasks/task/{Uri.EscapeDataString(taskId)}", null, ct)
                .ConfigureAwait(false);
            LastError = resp.IsSuccessStatusCode ? null : await ReadErrorAsync(resp, ct);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return false;
        }
    }

    // ---------- Results ----------

    public Task<ResultListResponseDto?> GetResultsAsync(string? problemId, string? taskId, CancellationToken ct = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(problemId)) query.Add("problemId=" + Uri.EscapeDataString(problemId));
        if (!string.IsNullOrWhiteSpace(taskId))    query.Add("taskId="    + Uri.EscapeDataString(taskId));
        var url = "Results/v1/results" + (query.Count > 0 ? "?" + string.Join('&', query) : "");
        return GetAsync<ResultListResponseDto>(url, ct);
    }

    // ---------- Internals ----------

    private async Task<T?> GetAsync<T>(string path, CancellationToken ct) where T : class
    {
        try
        {
            using var resp = await _http.GetAsync(path, ct).ConfigureAwait(false);
            if (resp.StatusCode == System.Net.HttpStatusCode.NoContent) { LastError = null; return null; }
            if (!resp.IsSuccessStatusCode) { LastError = await ReadErrorAsync(resp, ct); return null; }
            LastError = null;
            return await resp.Content.ReadFromJsonAsync<T>(_json, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return null;
        }
    }

    private async Task<TOut?> PostAsync<TIn, TOut>(string path, TIn body, CancellationToken ct)
        where TOut : class
    {
        try
        {
            using var resp = await _http.PostAsJsonAsync(path, body, _json, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) { LastError = await ReadErrorAsync(resp, ct); return null; }
            LastError = null;
            return await resp.Content.ReadFromJsonAsync<TOut>(_json, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return null;
        }
    }

    private async Task<bool> DeleteAsync(string path, CancellationToken ct)
    {
        try
        {
            using var resp = await _http.DeleteAsync(path, ct).ConfigureAwait(false);
            LastError = resp.IsSuccessStatusCode ? null : await ReadErrorAsync(resp, ct);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return false;
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        try
        {
            var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return string.IsNullOrWhiteSpace(body) ? $"HTTP {(int)resp.StatusCode}" : $"HTTP {(int)resp.StatusCode}: {body}";
        }
        catch
        {
            return $"HTTP {(int)resp.StatusCode}";
        }
    }
}
