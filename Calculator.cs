using System.Globalization;

namespace SessionDashboard;

internal static class Calculator
{
    internal static decimal Evaluate(string expression)
    {
        if (expression.Length > 256) throw new FormatException("Keep calculations under 256 characters.");
        var parser = new Parser(expression.Replace('×', '*').Replace('÷', '/').Replace('−', '-'));
        var result = parser.Sum(); parser.Skip();
        if (!parser.End) throw new FormatException("Check your calculation.");
        return result;
    }
    private sealed class Parser(string input)
    {
        private int position;
        internal bool End => position == input.Length;
        internal void Skip() { while (!End && char.IsWhiteSpace(input[position])) position++; }
        private bool Take(char token) { Skip(); if (End || input[position] != token) return false; position++; return true; }
        internal decimal Sum()
        {
            var value = Product();
            while (true) { if (Take('+')) value += Product(); else if (Take('-')) value -= Product(); else return value; }
        }
        private decimal Product()
        {
            var value = Number();
            while (true) { if (Take('*')) value *= Number(); else if (Take('/')) value /= Number(); else return value; }
        }
        private decimal Number()
        {
            Skip(); decimal value;
            if (Take('+')) return Number();
            if (Take('-')) return -Number();
            if (Take('(')) { value = Sum(); if (!Take(')')) throw new FormatException("Close the parenthesis."); }
            else
            {
                var start = position;
                while (!End && (char.IsAsciiDigit(input[position]) || input[position] == '.')) position++;
                if (!decimal.TryParse(input[start..position], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value)) throw new FormatException("Enter a number using a decimal point.");
            }
            while (Take('%')) value /= 100;
            return value;
        }
    }
}
