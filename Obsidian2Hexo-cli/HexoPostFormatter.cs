﻿using System.Text.RegularExpressions;

namespace Obsidian2Hexo;

using Adapter = HexoPostStyleAdapter;

internal class HexoPostFormatter
{
    private string m_SrcNotePath = null;
    private string m_DstPostPath = null;

    public HexoPostFormatter(string srcNotePath, string dstPostPath)
    {
        m_SrcNotePath = srcNotePath;
        m_DstPostPath = dstPostPath;
    }

    public void Format()
    {
        string content = File.ReadAllText(m_SrcNotePath);
        string ret = AdmonitionsFormatter.FormatCodeBlockStyle2ButterflyStyle(content);
        ret = AdmonitionsFormatter.FormatMkDocsStyle2ButterflyStyle(ret);
        ret = FormatBlockLink(ret, m_SrcNotePath);
        ret = CleanBlockLinkMark(ret);
        ret = FormatMdLinkToHexoStyle(ret);
        File.WriteAllText(m_DstPostPath, ret);
    }

    private string FormatBlockLink(string content, string srcNotePath)
    {
        // Block Link's path is related to the vault path, not the note path.

        string pattern = @"!\[(.*?)\]\((.*?.md)#(\^[a-zA-Z0-9]{6})\)";
        return Regex.Replace(content, pattern, ReplaceBlockLink);

        string ReplaceBlockLink(Match match)
        {
            string _ = match.Groups[1].Value;
            string linkRelativePath = match.Groups[2].Value;
            string blockId = match.Groups[3].Value;
            string targetNotePath
                = ObsidianNoteParser.GetNotePathBasedOnFolder(Obsidian2HexoHandler.obsidianTempDir.FullName,
                                                              linkRelativePath);

            if (!File.Exists(targetNotePath))
                targetNotePath = ObsidianNoteParser.GetAbsoluteLinkPath(srcNotePath, linkRelativePath);

            if (File.Exists(targetNotePath))
            {
                string blockContent = File.ReadAllLines(targetNotePath).ToList()
                                          .First(line => line.EndsWith(blockId)).Replace(blockId, "");

                string quoteContent = $"""
                                       {blockContent}
                                       ———— {GetReferenceLink()}
                                       """;

                return Adapter.AdaptAdmonition(quoteContent, "'fas fa-quote-left'");
            }

            Console.WriteLine($"Not Found for relative path {linkRelativePath}");
            Console.WriteLine($"Not Found for absolute path {targetNotePath}");
            return "";

            string GetReferenceLink()
            {
                string title = ObsidianNoteParser.GetTitle(targetNotePath);
                if (!ObsidianNoteParser.IsRequiredToBePublished(targetNotePath)) return title;

                string postPath = Adapter.AdaptPostPath(Adapter.ConvertMdLink2Relative(targetNotePath));
                return $"[{title}]({postPath})";
            }
        }
    }

    private string CleanBlockLinkMark(string content)
    {
        string pattern = @"(\^[a-zA-Z0-9]{6})";
        return Regex.Replace(content, pattern, "");
    }

    private string FormatMdLinkToHexoStyle(string content)
    {
        string linkPattern = @"\[(.*?)\]\((.*?)\)";
        string ret = Regex.Replace(content, linkPattern, ReplaceLink);
        return ret;

        string ReplaceLink(Match match)
        {
            string linkText = match.Groups[1].Value;
            string linkRelativePath = match.Groups[2].Value;

            return HexoPostStyleAdapter.AdaptLink(linkText, linkRelativePath, m_SrcNotePath, m_DstPostPath);
        }
    }
}