
namespace UnityExtensions
{
    /// <summary>
    /// State.
    /// </summary>
    public interface IState
    {
        /// <summary>
        /// Called when StateMachine enters this state.
        /// </summary>
        void OnEnter();

        /// <summary>
        /// Called when StateMachine quits this state.
        /// </summary>
        void OnExit();

        /// <summary>
        /// Called when StateMachine updates this state.
        /// </summary>
        void OnUpdate(float deltaTime);

    } // interface IState

} // namespace UnityExtensions
