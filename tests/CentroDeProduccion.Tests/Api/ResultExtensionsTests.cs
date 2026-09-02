using CentroDeProduccion.Api.Extensions;
using CentroDeProduccion.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace CentroDeProduccion.Tests.Api;

/// <summary>
/// Verifies <see cref="ResultExtensions"/> — the single seam mapping a failed
/// <see cref="Result"/> to an RFC 7807 ProblemDetails response (design D5). A minimal stub
/// controller stands in for a real endpoint so this test needs no HTTP host.
/// </summary>
public class ResultExtensionsTests
{
    private sealed class StubController : ControllerBase
    {
        public IActionResult NotFoundStub()
        {
            var result = Result<string>.Failure(Error.NotFound("Insumo.NotFound", "Insumo not found."));
            return result.ToActionResult(this);
        }

        public IActionResult SuccessStub()
        {
            var result = Result<string>.Success("ok");
            return result.ToActionResult(this);
        }
    }

    [Fact]
    public void ToActionResult_NotFoundError_Returns404ProblemDetails()
    {
        var controller = new StubController();

        var actionResult = controller.NotFoundStub();

        var objectResult = actionResult.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(404);
        var problemDetails = objectResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Status.ShouldBe(404);
        problemDetails.Detail.ShouldBe("Insumo not found.");
        problemDetails.Extensions["errorCode"].ShouldBe("Insumo.NotFound");
    }

    [Fact]
    public void ToActionResult_Success_ReturnsOkWithValue()
    {
        var controller = new StubController();

        var actionResult = controller.SuccessStub();

        var okResult = actionResult.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe("ok");
    }
}
