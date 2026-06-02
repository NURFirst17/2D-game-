public interface ICheckpointStateParticipant
{
    string CaptureCheckpointState();
    void RestoreCheckpointState(string state);
}
