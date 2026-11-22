using FluentValidation;
using HoneyHouse.Application.Common;
using HoneyHouse.Application.Localization;
using Shouldly;

namespace HoneyHouse.UnitTests.Application;

public class LocalizationAndRulesTests
{
    [Fact]
    public void Messages_are_returned_in_requested_language()
    {
        Messages.Get("store_name", "ar").ShouldBe("بيت العسل");
        Messages.Get("store_name", "en").ShouldBe("Honey House");
    }

    [Fact]
    public void Unknown_code_falls_back_to_code() => Messages.Get("no_such_code", "en").ShouldBe("no_such_code");

    private sealed record Phone(string? Value);
    private sealed class PhoneValidator : AbstractValidator<Phone>
    {
        public PhoneValidator() => RuleFor(x => x.Value).EgyptianMobile();
    }

    [Theory]
    [InlineData("01012345678", true)]
    [InlineData("01112345678", true)]
    [InlineData("01212345678", true)]
    [InlineData("01512345678", true)]
    [InlineData("+201012345678", true)]
    [InlineData("01312345678", false)]
    [InlineData("0101234567", false)]
    [InlineData("1012345678", false)]
    public void Egyptian_mobile_validation(string phone, bool valid) =>
        new PhoneValidator().Validate(new Phone(phone)).IsValid.ShouldBe(valid);

    [Fact]
    public void Result_carries_value_or_errors()
    {
        Result<int> ok = 5;
        ok.IsSuccess.ShouldBeTrue();
        ok.Value.ShouldBe(5);

        Result<int> fail = Error.NotFound("not_found");
        fail.IsFailure.ShouldBeTrue();
        fail.FirstError!.Type.ShouldBe(ErrorType.NotFound);
        Should.Throw<InvalidOperationException>(() => _ = fail.Value);
    }

    [Fact]
    public void Paged_request_clamps_values()
    {
        var r = new PagedRequest { Page = 0, PageSize = 1000 };
        r.Page.ShouldBe(1);
        r.PageSize.ShouldBe(PagedRequest.MaxPageSize);
        new PagedResult<int>([1, 2], 1, 2, 5).TotalPages.ShouldBe(3);
    }
}
