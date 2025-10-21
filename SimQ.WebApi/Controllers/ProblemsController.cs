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

    [HttpGet("problem/{problemId:guid}")]
    public async Task<ActionResult<ProblemResponse>> GetProblem([FromRoute] Guid problemId, CancellationToken cancellationToken)
    {
        var problem = await _problemService.GetProblemAsync(problemId.ToString(), cancellationToken);
        if (problem == null)
        {
            return NotFound($"Problem with id {problemId} was not found");
        }

        return problem;
    }

    [HttpDelete("problem/{problemId:guid}")]
    public async Task<IActionResult> DeleteProblem([FromRoute] Guid problemId, CancellationToken cancellationToken)
    {
        var result = await _problemService.DeleteProblem(problemId.ToString(), cancellationToken);
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
        if (response == null)
        {
            return BadRequest();
        }
        
        return response;
    }

    [HttpGet("problem/{problemId:guid}/results")]
    public async Task<ActionResult<ResultListResponse>> GetResults(
        [FromRoute] Guid problemId,
        CancellationToken cancellationToken)
    {
        var response = await _problemService.GetProblemResults(problemId.ToString(), cancellationToken);
        if (response == null) return NotFound($"Results with problem id {problemId} was not found");

        return response;
    }

    [HttpGet("problem/{problemId:guid}/results/{resultId:guid}")]
    public async Task<ActionResult<ResultDto>> GetResult(
        [FromRoute] Guid problemId,
        [FromRoute] Guid resultId,
        CancellationToken cancellationToken
    )
    {
        var response = await _problemService.GetResult(problemId.ToString(), resultId.ToString(), cancellationToken);
        if (response == null)
            return NotFound($"ResultDto with problem id {problemId} and result id {problemId} was not found");
        
        return response;
    }

    [HttpDelete("problem/{problemId:guid}/results/{resultId:guid}")]
    public async Task<IActionResult> DeleteResult(
        [FromRoute] Guid problemId,
        [FromRoute] Guid resultId,
        CancellationToken cancellationToken
    )
    {
        var result = await _problemService.DeleteResult(problemId.ToString(), resultId.ToString(), cancellationToken);
        if (!result) return NotFound($"ResultDto with problem id {problemId} and result id {problemId} was not found");

        return Ok();
    }
}