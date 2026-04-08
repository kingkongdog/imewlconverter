/*
 *   Copyright © 2009-2020 studyzy(深蓝,曾毅)

 *   This program "IME WL Converter(深蓝词库转换)" is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.

 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.

 *   You should have received a copy of the GNU General Public License
 *   along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Studyzy.IMEWLConverter.Helpers;

public static class PinyinHelper
{
    private static readonly Dictionary<char, char[]> ToneMap = new Dictionary<char, char[]>
    {
        {'a', new[] {'ā', 'á', 'ǎ', 'à', 'a'}},
        {'o', new[] {'ō', 'ó', 'ǒ', 'ò', 'o'}},
        {'e', new[] {'ē', 'é', 'ě', 'è', 'e'}},
        {'i', new[] {'ī', 'í', 'ǐ', 'ì', 'i'}},
        {'u', new[] {'ū', 'ú', 'ǔ', 'ù', 'u'}},
        {'v', new[] {'ǖ', 'ǘ', 'ǚ', 'ǜ', 'ü'}} // v 代表 ü
    };

    public static string ConvertToneNumbersToMarks(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // 匹配拼音结构的正则：[声母] + [韵母] + [1-5数字]
        return Regex.Replace(input.ToLower(), @"([a-zü]+)([1-5])", m =>
        {
            string pinyin = m.Groups[1].Value;
            int tone = int.Parse(m.Groups[2].Value);

            if (tone == 5) return pinyin; // 五声（轻声）不标调

            // 标调规则逻辑
            int markIndex = -1;
            if (pinyin.Contains("a")) markIndex = pinyin.IndexOf('a');
            else if (pinyin.Contains("o")) markIndex = pinyin.IndexOf('o');
            else if (pinyin.Contains("e")) markIndex = pinyin.IndexOf('e');
            else if (pinyin.Contains("ui")) markIndex = pinyin.IndexOf('i'); // ui 标在 i 上
            else if (pinyin.Contains("iu")) markIndex = pinyin.IndexOf('u'); // iu 标在 u 上
            else
            {
                // 剩余情况标在最后一个元音上（如 i, u, v）
                for (int i = pinyin.Length - 1; i >= 0; i--)
                {
                    if (ToneMap.ContainsKey(pinyin[i]))
                    {
                        markIndex = i;
                        break;
                    }
                }
            }

            if (markIndex != -1)
            {
                char targetChar = pinyin[markIndex];
                char markedChar = ToneMap[targetChar][tone - 1];
                return pinyin.Remove(markIndex, 1).Insert(markIndex, markedChar.ToString());
            }

            return pinyin + tone; // 如果没匹配到元音，原样返回
        });
    }

    /// <summary>
    ///     获得一个字的默认拼音(不包含音调)
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public static string GetDefaultPinyin(char c)
    {
        try
        {
            // Check if it's an English letter or other ASCII character
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
            {
                // Return the lowercase letter itself as the "pinyin"
                return c.ToString().ToLower();
            }

            // Check if it's a digit
            if (c >= '0' && c <= '9')
            {
                return c.ToString();
            }

            var pys = PinYinDict[c];
            if (pys != null && pys.Count > 0) return pys[0];
            throw new Exception($"找不到字:'{c}'的拼音");
        }
        catch
        {
            throw new Exception($"找不到字:'{c}'的拼音");
        }
    }

    public static IList<string> GetDefaultPinyin(string word)
    {
        var result = new List<string>();
        // Use StringInfo to properly handle surrogate pairs
        var si = new System.Globalization.StringInfo(word);
        for (int i = 0; i < si.LengthInTextElements; i++)
        {
            var textElement = si.SubstringByTextElements(i, 1);
            // Only process single char elements (BMP characters)
            if (textElement.Length == 1)
            {
                result.Add(GetDefaultPinyin(textElement[0]));
            }
            else
            {
                // For surrogate pairs or characters without pinyin, skip them
                // or handle them as empty pinyin
                Debug.WriteLine($"Skipping character beyond BMP: {textElement}");
            }
        }
        return result;
    }

    /// <summary>
    ///     获得单个字的拼音,不包括声调
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static IList<string> GetPinYinOfChar(char str)
    {
        return PinYinDict[str];
    }

    /// <summary>
    ///     判断一个字是否多音字
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public static bool IsMultiPinyinWord(char c)
    {
        return GetPinYinOfChar(c).Count > 1;
    }

    /// <summary>
    ///     获得单个字的拼音,包括声调
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static List<string> GetPinYinWithToneOfChar(char str)
    {
        return PinYinWithToneDict[str];
    }

    /// <summary>
    ///     如果给出一个字和一个没有音调的拼音，返回正确的带音调的拼音
    /// </summary>
    /// <param name="str"></param>
    /// <param name="py"></param>
    /// <returns></returns>
    public static string AddToneToPinyin(char str, string py)
    {
        if (!PinYinWithToneDict.ContainsKey(str))
        {
            Debug.WriteLine("找不到" + str + "的拼音,使用其默认拼音对应的音调1");
            return py + "1";
        }

        var list = PinYinWithToneDict[str];
        foreach (var allpinyin in list)
            foreach (var pinyin in allpinyin.Split(','))
                if (
                    pinyin == py + "0"
                    || pinyin == py + "1"
                    || pinyin == py + "2"
                    || pinyin == py + "3"
                    || pinyin == py + "4"
                    || pinyin == py + "5"
                )
                    return pinyin;

        Debug.WriteLine("找不到" + str + "的拼音" + py + "对应的音调");
        return py + "1"; //找不到音调就用拼音的一声
    }

    /// <summary>
    ///     判断给出的词和拼音是否有效
    /// </summary>
    /// <param name="word"></param>
    /// <param name="pinyin"></param>
    /// <returns></returns>
    public static bool ValidatePinyin(string word, List<string> pinyin)
    {
        var pinyinList = pinyin;
        if (word.Length != pinyinList.Count) return false;
        for (var i = 0; i < word.Length; i++)
        {
            var charPinyinList = GetPinYinOfChar(word[i]);
            if (!charPinyinList.Contains(pinyinList[i])) return false;
        }

        return true;
    }

    #region Init

    private static readonly Dictionary<char, List<string>> dictionary = new();
    private static readonly Dictionary<char, IList<string>> pyDictionary = new();

    /// <summary>
    ///     字的拼音(包括音调)
    /// </summary>
    private static Dictionary<char, List<string>> PinYinWithToneDict
    {
        get
        {
            if (dictionary.Count == 0)
            {
                var pyList = DictionaryHelper.GetAll();

                foreach (var code in pyList)
                {
                    var hz = code.Word;
                    var py = code.Pinyins;
                    if (!string.IsNullOrEmpty(py)) dictionary.Add(hz, new List<string>(py.Split(';')));
                }
            }

            return dictionary;
        }
    }

    /// <summary>
    ///     字的拼音，不包括音调
    /// </summary>
    public static Dictionary<char, IList<string>> PinYinDict
    {
        get
        {
            if (pyDictionary.Count == 0)
            {
                var pyList = DictionaryHelper.GetAll();

                foreach (var code in pyList)
                {
                    var hz = code.Word;
                    var pys = code.Pinyins;
                    if (!string.IsNullOrEmpty(pys))
                        foreach (var s in pys.Split(','))
                        {
                            var py = s.Remove(s.Length - 1); //remove tone
                            if (pyDictionary.ContainsKey(hz))
                            {
                                if (!pyDictionary[hz].Contains(py)) pyDictionary[hz].Add(py);
                            }
                            else
                            {
                                pyDictionary.Add(hz, new List<string> { py });
                            }
                        }
                }
            }

            return pyDictionary;
        }
    }

    #endregion

    ///// <summary>
    ///// 获得一个词中的每个字的音
    ///// </summary>
    ///// <param name="str">一个词</param>
    ///// <returns></returns>
    //public static List<List<string>> GetPinYinOfStringEveryChar(string str)
    //{
    //    var pyList = new List<List<string>>();
    //    for (int i = 0; i < str.Length; i++)
    //    {
    //        pyList.Add(GetPinYinOfChar(str[i]));
    //    }
    //    return pyList;
    //}
}
