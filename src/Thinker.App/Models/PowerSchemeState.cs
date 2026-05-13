namespace Thinker.Models;

public sealed record PowerSchemeState(
    string SchemeGuid,
    LidAction AcAction,
    LidAction DcAction);
