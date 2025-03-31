
namespace UnityExtensions
{
    /// <summary>
    /// Stack state.
    /// </summary>
    public interface IStackState
    {
        /// <summary>
        /// Called when StateMachine resets stack.
        /// </summary>
        void OnReset();

        /// <summary>
        /// Called when StateMachine pushs this state.
        /// </summary>
        void OnPush();

        /// <summary>
        /// Called when StateMachine pops this state.
        /// </summary>
        void OnPop();

        /// <summary>
        /// Called when StateMachine pushs other state if this was the current.
        /// </summary>
        void OnSuspend();

        /// <summary>
        /// Called when StateMachine pops other state if this becomes the current.
        /// </summary>
        void OnResume();

        /// <summary>
        /// Called when StateMachine updates this state.
        /// </summary>
        void OnUpdate(float deltaTime);

    } // interface IStackState

} // namespace UnityExtensions
