public interface IWakgamesTokenStorage
{
    /// <summary>
    /// ���� ��ū.
    /// </summary>
    public string AccessToken { get; }
    /// <summary>
    /// ���� ��ū.
    /// </summary>
    public string RefreshToken { get; }
    /// <summary>
    /// ����� ID.
    /// </summary>
    public int IdToken { get; }

    /// <summary>
    /// ��ū ����.
    /// </summary>
    /// <param name="accessToken">���� ��ū.</param>
    /// <param name="refreshToken">���� ��ū.</param>
    /// <param name="idToken">����� ID.</param>
    public void UpdateToken(string accessToken, string refreshToken, int idToken);
    /// <summary>
    /// ��ū ����.
    /// </summary>
    public void ClearToken();
}
