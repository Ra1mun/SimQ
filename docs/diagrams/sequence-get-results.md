# Цепочка вызовов: Получение результатов имитационного моделирования

## Прямой путь (запрос)

```
MainViewModel.OpenResultsCommand()
  → MainViewModel: Screen = AppScreen.Results

MainViewModel.RefreshResultsAsync()
  → SimQApiClient.GetResultsAsync(problemId, taskId)
    → SimQApiClient.GetAsync<ResultListResponseDto>("Results/v1/results?problemId=...&taskId=...")
      → HttpClient.GetAsync(path, ct)
        ─── HTTP GET /Results/v1/results ───►
        → ResultsController.GetResults(problemId?, taskId?, ct)
          → ResultService.GetAllResultsAsync(problemId, taskId, ct)
            → ResultRepository.GetManyAsync(BaseRequest<Result> { Predicate = r => r.ProblemId == problemId }, ct)
              → MongoDB.Find(filter)
```

## Обратный путь (ответ)

```
              MongoDB → List<Result>
            ← ResultRepository → List<Result>
          ← ResultService: _mapper.Map<ResultDto>(result)  [для каждого Result через AutoMapper / ResultProfile]
          ← ResultService → ResultListResponse { Results = ResultDto[] }
        ← ResultsController → ActionResult<ResultListResponse>
        ◄─── HTTP 200 JSON ───
      ← HttpClient → HttpResponseMessage
    ← SimQApiClient: ReadFromJsonAsync<ResultListResponseDto>()
  ← SimQApiClient → ResultListResponseDto
← MainViewModel: заполняет ResultTable (вычисляет BarWidth, ChartHeight, CDF)
← ResultsView.axaml: ItemsSource="{Binding ResultTable}" → отрисовка
```

## Используемые сущности на каждом шаге

```
[MainViewModel]          → AppScreen, ObservableCollection<ResultRow>
[SimQApiClient]          → ResultListResponseDto, HttpClient, JsonSerializerOptions
[ResultsController]      → query-параметры problemId, taskId
[ResultService]          → BaseRequest<Result>, IMapper, List<Result>
[AutoMapper]             → Result → ResultDto
                           SimulationResultData → SimulationResultDataDto
                           AgentStatisticResult → AgentStatisticResultDto
[ResultRepository]       → MongoDB-коллекция "results", документ Result
[MainViewModel расчёт]   → ResultRow { N, P, Cdf, BarWidth, ChartHeight }
[ResultsView отрисовка]  → ItemsControl, UniformGrid, Border (высота/ширина по привязке)
```
