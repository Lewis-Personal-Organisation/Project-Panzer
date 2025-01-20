using System.Text.RegularExpressions;

public static class RelayData
{
	public readonly static int maxCodeLength = 6;
	private readonly static Regex regex = new("^[A-Z0-9]*$");
	public static bool CodeIsValid(string gameCode) => gameCode.Length == maxCodeLength && regex.IsMatch(gameCode);
}