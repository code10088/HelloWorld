using System;
using System.Collections.Generic;

namespace HotAssembly
{
	public static class Utils
	{
		private static readonly string[] splitStr = new string[] { "(", ")", "!", "&&", "||" };

		public static List<int> SplitBoolStr(string str)
        {
			List<int> result = new List<int>();
			var strs = str.Split(splitStr, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < strs.Length; i++)
            {
				int a = int.Parse(strs[i]);
				if (!result.Contains(a)) result.Add(a);
			}
			result.Sort((a, b) => { return b - a; });
			return result;
		}
		/// <summary>
		/// ()!&|解析，非括号内的执行优先级从后往前
		/// </summary>
		/// <returns></returns>
		public static bool ParseBoolStr(string str)
		{
			if(string.IsNullOrEmpty(str)) return true;
			Stack<char> stack = new Stack<char>();
			int n = str.Length;
			for (int i = 0; i < n; i++)
			{
				char c = str[i];
				stack.Push(c);
				if (c == ')')
				{
					bool b = false;
					c = stack.Pop();
					while (true)
					{
						if (c == '(') break;
						else if (c == 't') b = true;
						else if (c == 'f') b = false;
						else if (c == '!') stack.Push(b ? 'f' : 't');
						else if (c == '&') stack.Push(stack.Pop() == 't' && b ? 't' : 'f');
						else if (c == '|') stack.Push(stack.Pop() == 't' || b ? 't' : 'f');
						c = stack.Pop();
					}
					stack.Push(b ? 't' : 'f');
				}
			}
			return stack.Pop() == 't';
		}
	}
}