using SatisfactorySdk.DTO;

namespace SatisfactoryServerGUI.Services;

public static class ClientResponseExtensions
{
    /// <summary>Human-readable error text for a failed/errored response.</summary>
    public static string ErrorText<T>(this ClientResponse<T> response)
    {
        if (response.ErrorResponse is { } err)
        {
            if (!string.IsNullOrWhiteSpace(err.ErrorMessage))
                return err.ErrorMessage!;
            if (!string.IsNullOrWhiteSpace(err.ErrorCode))
                return err.ErrorCode;
        }

        return $"Request failed ({(int)response.StatusCode} {response.StatusCode}).";
    }

    /// <summary>True when the call completed and the server did not return an error.</summary>
    public static bool Ok<T>(this ClientResponse<T> response)
        => response.IsSuccessful && !response.HasError;
}
