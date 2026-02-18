using System.Text;

public static class StringExtension
{
    public static int LastIndex(this string input) => input.Length - 1;
    public static int LastIndex(this StringBuilder input) => input.Length - 1;

    public static int First(this string input) => input[0];
    public static int First(this StringBuilder input) => input[0];

    public static int Last(this string input) => input[input.Length - 1];
    public static int Last(this StringBuilder input) => input[input.Length - 1];

    public static string TrimEdge(this string input)
    {
        StringBuilder sb = new(input);

        // Remove starting spaces
        while (sb.Length > 0 && sb.First() == ' ')
            sb.Remove(0, 1);


        while (sb.Length > 0 && sb.Last() == ' ')
            sb.Remove(sb.LastIndex(), 1);

        return sb.ToString();
    }

    public static string TrimEdge(this StringBuilder input)
    {
        StringBuilder sb = input;

        // Remove starting spaces
        while (sb.Length > 0 && sb[0] == ' ')
            sb.Remove(0, 1);


        while (sb.Length > 0 && sb.Last() == ' ')
            sb.Remove(sb.LastIndex(), 1);

        return sb.ToString();
    }

    /// <summary>
    /// Removes Unity specific suffixes and spaces for OBJ compatibility.
    /// </summary>
    /// <param name="inString">The string to clean (GameObject or Material name)</param>
    /// <returns>A cleaned string with underscores instead of spaces</returns>
    static public string ClearUnityString(this string inString)
    {
        if (string.IsNullOrEmpty(inString)) return "Undefined";

        // Remove (Instance) and (Clone)
        string outString = inString.Replace("(Instance)", "").Replace("(Clone)", "");


        // Trim any leftover whitespace
        return outString.TrimEdge();
    }

    static public string RemoveModID(this string input)
    {
        // Remove mod suffix if present to find original MTL
        // Example: "Chair#m1" -> "Chair"
        int hashIndex = input.IndexOf('#');
        return (hashIndex != -1) ? input.Substring(0, hashIndex) : input;
    }
}
