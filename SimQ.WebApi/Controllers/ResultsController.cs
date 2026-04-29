using Microsoft.AspNetCore.Mvc;
using SimQ.Core.Dtos.Out;
using SimQ.Core.Services;

namespace SimQ.WebApi.Controllers;

[ApiController]
[Route("[controller]/v1")]
public class ResultsController : ControllerBase
{
    private readonly IResultService _resultService;

    public ResultsController(IResultService resultService)
    {
        _resultService = resultService;
    }

    [HttpGet("results")]
    public async Task<ActionResult<ResultListResponse>> GetResults(
        [FromQuery] string? problemId,
        [FromQuery] string? taskId,
        CancellationToken cancellationToken)
    {
        var response = await _resultService.GetAllResultsAsync(problemId, taskId, cancellationToken);
        if (response.Results.Length == 0) return NoContent();

        return response;
    }

    [HttpGet("result/{resultId}")]
    public async Task<ActionResult<ResultDto>> GetResult(
        [FromRoute] string resultId,
        CancellationToken cancellationToken)
    {
        var response = await _resultService.GetResultAsync(resultId, cancellationToken);
        if (response == null) return NotFound($"Result with id {resultId} was not found");

        return response;
    }

    [HttpDelete("result/{resultId}")]
    public async Task<IActionResult> DeleteResult(
        [FromRoute] string resultId,
        CancellationToken cancellationToken)
    {
        var result = await _resultService.DeleteResultAsync(resultId, cancellationToken);
        if (!result) return NotFound($"Result with id {resultId} was not found");

        return Ok();
    }
}
