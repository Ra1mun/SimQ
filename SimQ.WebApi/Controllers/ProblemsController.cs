using Microsoft.AspNetCore.Mvc;
using SimQ.Core.Dtos.In;
using SimQ.Core.Dtos.Out;
using SimQ.Core.Services;

namespace SimQ.WebApi.Controllers;

[ApiController]
[Route("[controller]/v1")]
public class ProblemsController : ControllerBase
{
    private readonly IProblemService _problemService;

    public ProblemsController(
        IProblemService problemService)
    {
        _problemService = problemService;
    }

    [HttpGet("problems")]
    public async Task<ActionResult<ProblemListReponse>> GetAllProblems(CancellationToken cancellationToken)
    {
        var response = await _problemService.GetAllProblemsAsync(cancellationToken);
        if (response.Total == 0) return NoContent();

        return response;
    }

    [HttpGet("problem/{problemId}")]
    public async Task<ActionResult<ProblemResponse>> GetProblem([FromRoute] string problemId,
        CancellationToken cancellationToken)
    {
        var problem = await _problemService.GetProblemAsync(problemId, cancellationToken);
        if (problem == null) return NotFound($"Problem with id {problemId} was not found");

        return problem;
    }

    [HttpDelete("problem/{problemId}")]
    public async Task<IActionResult> DeleteProblem([FromRoute] string problemId, CancellationToken cancellationToken)
    {
        var result = await _problemService.DeleteProblem(problemId, cancellationToken);
        if (!result) return NotFound($"Problem with id {problemId} was not found");

        return Ok();
    }

    [HttpPost("problem")]
    public async Task<ActionResult<RegisterProblemResponse>> RegisterProblem(
        [FromBody] RegisterProblemRequest request,
        CancellationToken cancellationToken
    )
    {
        var response = await _problemService.RegisterProblem(request, cancellationToken);
        if (response == null) return BadRequest();

        return response;
    }

    [HttpGet("problem/{problemId}/results")]
    public async Task<ActionResult<ResultListResponse>> GetResults(
        [FromRoute] string problemId,
        CancellationToken cancellationToken)
    {
        var response = await _problemService.GetProblemResults(problemId, cancellationToken);
        if (response == null) return NotFound($"Results with problem id {problemId} was not found");

        return response;
    }

    [HttpGet("problem/{problemId}/results/{resultId}")]
    public async Task<ActionResult<ResultDto>> GetResult(
        [FromRoute] string problemId,
        [FromRoute] string resultId,
        CancellationToken cancellationToken
    )
    {
        var response = await _problemService.GetResult(problemId, resultId, cancellationToken);
        if (response == null)
            return NotFound($"ResultDto with problem id {problemId} and result id {problemId} was not found");

        return response;
    }

    [HttpDelete("problem/{problemId}/results/{resultId}")]
    public async Task<IActionResult> DeleteResult(
        [FromRoute] string problemId,
        [FromRoute] string resultId,
        CancellationToken cancellationToken
    )
    {
        var result = await _problemService.DeleteResult(problemId, resultId, cancellationToken);
        if (!result) return NotFound($"ResultDto with problem id {problemId} and result id {problemId} was not found");

        return Ok();
    }
}