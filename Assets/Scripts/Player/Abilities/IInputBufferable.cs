/// <summary>
/// Optional interface for abilities that can buffer input (e.g. light attack combo).
/// When the player presses the ability button while the ability cannot perform yet,
/// the manager calls TryBufferInput so the ability can queue one input for later.
/// </summary>
public interface IInputBufferable
{
    /// <summary>
    /// Called when the ability button was pressed but CanPerform is false.
    /// Return true if the input was buffered (consumed).
    /// </summary>
    bool TryBufferInput();
}
