using FluentAssertions;
using WebApp.Common;

namespace WebApp.UnitTests.Common;

public class ApiResponseTests
{
    [Fact]
    public void SuccessResponse_Generic_SetsSuccessAndData()
    {
        var resp = ApiResponse<int>.SuccessResponse(123, "ok");

        resp.Success.Should().BeTrue();
        resp.Data.Should().Be(123);
        resp.Message.Should().Be("ok");
        resp.Errors.Should().BeNull();
    }

    [Fact]
    public void ErrorResponse_Generic_SetsFailureAndErrors()
    {
        var resp = ApiResponse<int>.ErrorResponse("bad", new List<string> { "e1" });

        resp.Success.Should().BeFalse();
        resp.Data.Should().Be(default(int)); // value type default (0)
        resp.Message.Should().Be("bad");
        resp.Errors.Should().BeEquivalentTo(new[] { "e1" });
    }

    [Fact]
    public void SuccessResponse_NonGeneric_SetsSuccess()
    {
        var resp = ApiResponse.SuccessResponse("done");

        resp.Success.Should().BeTrue();
        resp.Message.Should().Be("done");
        resp.Errors.Should().BeNull();
    }

    [Fact]
    public void ErrorResponse_NonGeneric_SetsFailureAndErrors()
    {
        var resp = ApiResponse.ErrorResponse("oops", new List<string> { "a", "b" });

        resp.Success.Should().BeFalse();
        resp.Message.Should().Be("oops");
        resp.Errors.Should().BeEquivalentTo(new[] { "a", "b" });
    }
}
