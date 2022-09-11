using System.Security.Cryptography;

namespace MultiplayerP2P;

/// <summary>
/// This prevents bots creating thousands of ghost servers.
/// </summary>
public static class Security
{
    /// <summary>
    /// Generate a question for the server to solve.
    /// </summary>
    /// <returns>Question packet</returns>
    public static byte[] GenerateQuestion()
    {
        using var memory = new MemoryStream();
        using var writer = new BinaryWriter(memory);
        for (var i = 0; i < Configuration.Data.Security.ChecksAmount; i++)
            writer.Write(RandomNumberGenerator.GetInt32(int.MaxValue));
        return memory.ToArray();
    }

    /// <summary>
    /// Verify the answer to the question sent.
    /// </summary>
    /// <param name="question">Question packet</param>
    /// <param name="answer">Answer packet</param>
    /// <returns>Success or not</returns>
    public static bool VerifyAnswer(byte[] question, byte[] answer)
    {
        using var questionMemory = new MemoryStream(question);
        using var questionReader = new BinaryReader(questionMemory);
        using var answerMemory = new MemoryStream(answer);
        using var answerReader = new BinaryReader(answerMemory);
        
        // Read the question packet
        var questions = new List<int>();
        for (var i = 0; i < Configuration.Data.Security.ChecksAmount; i++)
            questions.Add(questionReader.ReadInt32());

        var num = Configuration.Data.Security.Numbers;
        
        // Verify the answer
        for (var i = 0; i < questions.Count; i++) {
            var q = questions[i];
            if (answerReader.ReadInt32() != q + num[i][0] * num[i][1] / num[i][2] - num[i][3])
                return false;
        }

        return true;
    }

    /// <summary>
    /// Authenticate a user. MODIFY THIS!
    /// </summary>
    /// <param name="data">User data</param>
    /// <returns>Success or not</returns>
    public static bool Authenticate(byte[] data)
        => true;
}