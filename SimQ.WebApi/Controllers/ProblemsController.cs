using Microsoft.AspNetCore.Mvc;
using SimQ.Core.Dtos;
using SimQ.Core.Dtos.In;
using SimQ.Core.Dtos.Out;
using SimQ.Core.Services;

namespace SimQ.WebApi.Controllers;

[ApiController]
[Route("[controller]/v1")]
public class ProblemsController : ControllerBase
{
    private readonly IProblemService _problemService;
    private readonly ILogger<ProblemsController> _logger;

    public ProblemsController(ILogger<ProblemsController> logger,
        IProblemService problemService)
    {
        _logger = logger;
        _problemService = problemService;
    }

    [HttpGet("problems")]
    public async Task<ActionResult<ProblemListReponse>> GetAllProblems()
    {
        var response = await _problemService.GetAllProblemsAsync();
        if (response.Total == 0) return NoContent();

        return response;
    }

    [HttpGet("problem/{problemId}")]
    public async Task<ActionResult<ProblemResponse>> GetProblem([FromRoute] Guid problemId)
    {
        var problem = await _problemService.GetProblemAsync(problemId.ToString());
        if (problem == null)
        {
            return NotFound($"Problem with id {problemId} was not found");
        }

        return problem;
    }

    [HttpDelete("problem/{problemId}")]
    public async Task<IActionResult> DeleteProblem([FromRoute] Guid problemId)
    {
        var result = await _problemService.DeleteProblem(problemId.ToString());
        if (!result) return NotFound($"Problem with id {problemId} was not found");

        return Ok();
    }

    [HttpPost("problem")]
    public async Task<ActionResult<RegisterProblemResponse>> RegisterProblem(
        [FromBody] RegisterProblemRequest request
    )
    {
        var response = await _problemService.RegisterProblem(request);
        if (response == null)
        {
            return BadRequest();
        }
        
        return response;
    }

    [HttpGet("problem/{problem_name}/results")]
    public async Task<ActionResult<ResultListResponse>> GetResults([FromRoute] Guid problemId)
    {
        var response = await _problemService.GetProblemResults(problemId.ToString());
        if (response == null) return NotFound($"Results with problem id {problemId} was not found");

        return response;
    }

    [HttpGet("problem/{problem_name}/results/{result_id}")]
    public async Task<ActionResult<ResultDto>> GetResult(
        [FromRoute] Guid problemId,
        [FromRoute] Guid resultId
    )
    {
        var response = await _problemService.GetResult(problemId.ToString(), resultId.ToString());
        if (response == null)
            return NotFound($"ResultDto with problem id {problemId} and result id {problemId} was not found");
        
        return response;
    }

    [HttpDelete("problem/{problem_name}/results/{result_id}")]
    public async Task<IActionResult> DeleteResult(
        [FromRoute] Guid problemId,
        [FromRoute] Guid resultId
    )
    {
        var result = await _problemService.DeleteResult(problemId.ToString(), resultId.ToString());
        if (!result) return NotFound($"ResultDto with problem id {problemId} and result id {problemId} was not found");

        return Ok();
    }
}