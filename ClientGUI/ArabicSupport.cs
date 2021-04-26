using System;
using System.Collections.Generic;
using System.Linq;

namespace ClientGUI
{
	public class ArabicText
	{
		String init = "ﺍﺑﺗﺛﺟﺣﺧﺩﺫﺭﺯﺳﺷﺻﺿﻁﻅﻋﻏﻓﻗﻛﻟﻣﻧﻫﻭﻳﻯﺁﺃﺇﺅﺀﺓﺋﻻﻵﻷﻹ";
		String mid = "ﺎﺒﺘﺜﺠﺤﺨﺪﺬﺮﺰﺴﺸﺼﻀﻄﻈﻌﻐﻔﻘﻜﻠﻤﻨﻬﻮﻴﻰﺂﺄﺈﺆﺀﺔﺌﻼﻶﻸﻺ";
		String last = "ﺎﺐﺖﺚﺞﺢﺦﺪﺬﺮﺰﺲﺶﺺﺾﻂﻆﻊﻎﻒﻖﻚﻞﻢﻦﻪﻮﻲﻰﺂﺄﺈﺆﺀﺔﺊﻼﻶﻸﻺ";
		String alefs = "اآأإ";
		String las = "ﻻﻵﻷﻹ";
		String separators = " :;,!?؟،.\'\"";

		/// <summary>
		/// Fixs the shape and the direction of arabic texts.
		/// </summary>
		/// <param name="text">The text to be fixed (LTR) e.g. (ل ا ث م).</param>
		/// <returns>The fixed text (RTL) e.g. (مثال).</returns>
		public string FixArabicText(string text)
		{
			String result = "";

			for (int i = text.Length - 1; i >= 0; i--)
				result += text[i];

			LinkedList<char> seps = new LinkedList<char>();

			for (int i = 0; i < result.Length; i++)
				if (separators.Contains(result[i]))
					seps.AddLast(result[i]);

			String[] words = result.Split(separators.ToCharArray());
			result = "";

			for (int i = 0; i < words.Length; i++)
			{
				String word = words[i];

				for (int j = 0; j < alefs.Length; j++)
				{
					String rep1 = alefs[j] + "ل";
					word = word.Replace(rep1, las[j] + "");
				}

				String newWord = "";

				if (word.Length > 1 && IsPureArabic(word))
				{
					for (int j = 0; j < word.Length; j++)
					{
						if (j == 0)
						{
							if (IsCut(word[j + 1]))
							{
								newWord += word[j];
							}
							else
							{
								newWord += last[GetIndex(word[j])];
							}
						}
						else if (j == word.Length - 1)
						{
							newWord += init[GetIndex(word[j])];
						}
						else
						{
							if (IsCut(word[j + 1]))
							{
								newWord += init[GetIndex(word[j])];
							}
							else
							{
								newWord += mid[GetIndex(word[j])];
							}
						}
					}
				}
				else
				{
					for (int x = word.Length - 1; x >= 0; x--)
					{
						newWord += word[x];
					}
				}

				result += newWord + (i < seps.Count ? seps.ElementAt(i) + "" : " ");
			}

			return result;
		}

		private bool IsCut(char c)
		{
			int x = GetIndex(c);
			return x == 0 || (x >= 7 && x <= 10) || x == 26 || (x >= 29 && x <= 34) || (x >= 36 && x <= 39);
		}

		private bool IsPureArabic(String word)
		{
			for (int i = 0; i < word.Length; i++)
			{
				if (GetIndex(word[i]) == -1)
				{
					return false;
				}
			}

			return true;
		}

		private int GetIndex(char c)
		{
			switch (c)
			{
				case 'ا': return 0;
				case 'ب': return 1;
				case 'ت': return 2;
				case 'ث': return 3;
				case 'ج': return 4;
				case 'ح': return 5;
				case 'خ': return 6;
				case 'د': return 7;
				case 'ذ': return 8;
				case 'ر': return 9;
				case 'ز': return 10;
				case 'س': return 11;
				case 'ش': return 12;
				case 'ص': return 13;
				case 'ض': return 14;
				case 'ط': return 15;
				case 'ظ': return 16;
				case 'ع': return 17;
				case 'غ': return 18;
				case 'ف': return 19;
				case 'ق': return 20;
				case 'ك': return 21;
				case 'ل': return 22;
				case 'م': return 23;
				case 'ن': return 24;
				case 'ه': return 25;
				case 'و': return 26;
				case 'ي': return 27;
				case 'ى': return 28;
				case 'آ': return 29;
				case 'أ': return 30;
				case 'إ': return 31;
				case 'ؤ': return 32;
				case 'ء': return 33;
				case 'ة': return 34;
				case 'ئ': return 35;
				case 'ﻻ': return 36;
				case 'ﻵ': return 37;
				case 'ﻷ': return 38;
				case 'ﻹ': return 39;
			}

			return -1;
		}
	}
}