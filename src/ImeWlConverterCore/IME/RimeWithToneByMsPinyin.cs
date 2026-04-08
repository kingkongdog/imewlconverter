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
using System.Text;
using System.Xml;
using Studyzy.IMEWLConverter.Entities;
using Studyzy.IMEWLConverter.Helpers;
using System.Text.RegularExpressions;

namespace Studyzy.IMEWLConverter.IME;

[ComboBoxShow(ConstantString.RIME_WITH_TONE_BY_MS_PINYIN, ConstantString.RIME_C_WITH_TONE_BY_MS_PINYIN,  1)]
public class RimeWithToneByMsPinyin : BaseImport, IWordLibraryExport, IWordLibraryTextImport
{
    #region IWordLibraryExport 成员

    public Encoding Encoding => Encoding.UTF8;

    public string ExportLine(WordLibrary wl)
    {
        return $"{wl.Word}\t{GetPinyinWithTone(wl)}\t{wl.Rank}";
    }

    public IList<string> Export(WordLibraryList wlList)
    {
        var sb = new StringBuilder();
        sb.Append(
            "# 网络流行新词" + "\n" +
            "# http://pinyin.sogou.com/dict/detail/index/4" + "\n" +
            "# https://github.com/kingkongdog/imewlconverter" + "\n" +
            "---" + "\n" +
            "name: wangluoliuxing" + "\n" +
            "version: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n" +
            "sort: by_weight" + "\n" +
            "..." + "\n"
        );
        
        for (var i = 0; i < wlList.Count; i++)
            try
            {
                sb.Append(ExportLine(wlList[i]));
                sb.Append("\n");
            }
            catch
            {
            }

        return new List<string> { sb.ToString() };
    }

    private string GetPinyinWithTone(WordLibrary wl)
    {
        string pattern = @"[\u4e00-\u9fa5]|[a-zA-Z]+";
        MatchCollection matches = Regex.Matches(wl.Word, pattern);  // 把 "阿福app" 拆分成 阿、福、app 三段
        
        var sb = new StringBuilder();
        var length = 0;

        for (int i = 0; i < matches.Count; i++)
        {
            var segment = matches[i].Value;

            var firstChar = segment[0];
            if ((firstChar >= 'a' && firstChar <= 'z') || (firstChar >= 'A' && firstChar <= 'Z'))   // 英文单词
            {
                sb.Append(segment + "5");
                length += segment.Length;
            }
            else    // 中文
            {
                var py = wl.PinYin[length];
                var pinyin = PinyinHelper.AddToneToPinyin(segment[0], py);
                if (pinyin == null) throw new Exception("找不到字[" + c + "]的拼音");
                sb.Append(pinyin);
                length++;
            }
            if (i != matches.Count - 1) sb.Append(" ");
        }
        
        return PinyinHelper.ConvertToneNumbersToMarks(sb.ToString());
    }

    #endregion

    #region IWordLibraryImport 成员

    public WordLibraryList Import(string path)
    {
        var str = FileOperationHelper.ReadFile(path, Encoding);
        return ImportText(str);
    }

    public WordLibraryList ImportText(string str)
    {
        var wlList = new WordLibraryList();
        return wlList;
    }

    public WordLibraryList ImportLine(string line)
    {
        throw new NotImplementedException();
    }

    #endregion
}
