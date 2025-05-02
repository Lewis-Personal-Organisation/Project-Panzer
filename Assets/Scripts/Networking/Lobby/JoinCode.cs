using System.Text.RegularExpressions;

public static class JoinCode
{
	public readonly static int maxLength = 6;
	private readonly static Regex regex = new("^[A-Z0-9]*$");
	public static bool IsValid(string joinCode) => joinCode.Length == maxLength && regex.IsMatch(joinCode);
}