using CoroMES.Integration.Cti.Models;

namespace CoroMES.Integration.Cti.Abstractions;

public interface ICtiParsedFileValidator
{
    IReadOnlyList<CtiValidationIssue> Validate(CtiParsedFile parsedFile);
}
