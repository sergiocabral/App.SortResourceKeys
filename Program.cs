using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

//***********************************************************************************************************
/// <summary>
/// Sorts resource keys in a source file and writes the sorted result.
/// </summary>
//***********************************************************************************************************
class Program
{
    //=======================================================================================================
    /// <summary>
    /// Main method of the application, scans all files and transforms them according to plan
    /// </summary>
    //=======================================================================================================
    static void Main()
    {
        foreach (var fileResx in new DirectoryInfo(Environment.CurrentDirectory).GetFiles())
        {
            if (!string.Equals(fileResx.Extension, ".resx", StringComparison.InvariantCultureIgnoreCase))
                continue;

            var fileCSharp = new FileInfo(Regex.Replace(fileResx.FullName, @".resx$", ".Designer.cs", RegexOptions.IgnoreCase));

            var fileResxContent = File.ReadAllText(fileResx.FullName, DetermineEncoding(fileResx.FullName));
            var fileCSharpContent = fileCSharp.Exists ? File.ReadAllLines(fileCSharp.FullName, DetermineEncoding(fileCSharp.FullName)) : new string[0];

            fileCSharpContent = SortCSharp(fileCSharpContent);
            fileResxContent = SortResx(fileResxContent);

            try
            {
                File.WriteAllText(fileResx.FullName, fileResxContent, DetermineEncoding(fileResx.FullName));
                if (fileCSharp.Exists)
                    File.WriteAllLines(fileCSharp.FullName, fileCSharpContent, DetermineEncoding(fileCSharp.FullName));
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

    //=======================================================================================================
    /// <summary>
    /// Determines encoding of the input file, assumes it should be at least UTF-8 without signature
    /// </summary>
    /// <param name="filePath">Path of the file</param>
    /// <returns>Assumed encoding of the input file</returns>
    //=======================================================================================================
    private static Encoding DetermineEncoding(string filePath)
    {
        var fileBytes = File.ReadAllBytes(filePath);

        if (fileBytes.Length >= 4 &&
            fileBytes[0] == 0x00 &&
            fileBytes[1] == 0x00 &&
            fileBytes[2] == 0xFE &&
            fileBytes[3] == 0xFF)
        {
            return new UTF32Encoding(true, true);
        }

        if (fileBytes.Length >= 4 &&
            fileBytes[0] == 0xFF &&
            fileBytes[1] == 0xFE &&
            fileBytes[2] == 0x00 &&
            fileBytes[3] == 0x00)
        {
            return new UTF32Encoding(false, true);
        }

        if (fileBytes.Length >= 2 &&
            fileBytes[0] == 0xFE &&
            fileBytes[1] == 0xFF)
        {
            return new UnicodeEncoding(true, true);
        }

        if (fileBytes.Length >= 2 &&
            fileBytes[0] == 0xFF &&
            fileBytes[1] == 0xFE)
        {
            return new UnicodeEncoding(false, true);
        }

        if (fileBytes.Length >= 3 &&
            fileBytes[0] == 0xEF &&
            fileBytes[1] == 0xBB &&
            fileBytes[2] == 0xBF)
        {
            return new UTF8Encoding(true);
        }

        return new UTF8Encoding(false);
    }

    //=======================================================================================================
    /// <summary>
    /// Identification for part of C# file
    /// </summary>
    private enum Mode
    {
        Header,
        Body,
        Footer
    }

    //=======================================================================================================
    /// <summary>
    /// Sorts C# file (Designer.cs)
    /// </summary>
    /// <param name="lines">Original lines</param>
    /// <returns>Sorted lines</returns>
    //=======================================================================================================
    private static string[] SortCSharp(string[] lines)
    {
        var header = new List<string>();
        var body = new List<List<string>>();
        var footer = new List<string>();

        var mode = Mode.Header;
        var i = 0;

        foreach (var line in lines)
        {
            switch (mode)
            {
                case Mode.Header:
                    if (!Regex.IsMatch(line, @"public\s*static\s*string\s*[^ ]*\s*{"))
                    {
                        header.Add(line);
                    }
                    else
                    {
                        body.Add(new List<string>());

                        while (header[header.Count - 1].Trim() != "}")
                        {
                            body[body.Count - 1].Insert(0, header[header.Count - 1]);
                            header.RemoveAt(header.Count - 1);
                        }

                        mode = Mode.Body;
                        goto case Mode.Body;
                    }

                    break;
                case Mode.Body:
                    if (line.Trim() == "}") i++;
                    else i = 0;

                    if (i <= 2)
                    {
                        body[body.Count - 1].Add(line);
                    }
                    if (i == 2)
                    {
                        body.Add(new List<string>());
                    }

                    if (i > 2)
                    {
                        body.RemoveAt(body.Count - 1);
                        mode = Mode.Footer;
                        goto case Mode.Footer;
                    }

                    break;
                case Mode.Footer:
                    footer.Add(line);
                    break;
            }
        }

        return Join(header, body, footer);
    }

    //=======================================================================================================
    /// <summary>
    /// Sorts Resx file
    /// </summary>
    /// <param name="content">Original content</param>
    /// <returns>Sorted Resx</returns>
    //=======================================================================================================
    private static string SortResx(string content)
    {
        var xml = new XmlDocument();
        xml.LoadXml(content);
        var nodeParent = xml.ChildNodes[1];
        var nodesToSort = new List<XmlNode>();
        foreach (var node in nodeParent.Cast<XmlNode>().ToList())
        {
            var xmlElement = node as XmlElement;
            if (xmlElement == null || xmlElement.Name != "data") continue;
            nodeParent.RemoveChild(node);
            nodesToSort.Add(xmlElement);
        }

        var nodesSorted = nodesToSort.OrderBy(a => a.Attributes["name"].Value).ToList();
        foreach (var node in nodesSorted)
        {
            nodeParent.AppendChild(node);
        }

        using (var memoryStream = new MemoryStream())
        using (var xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8))
        {
            xmlTextWriter.Formatting = Formatting.Indented;
            xml.WriteContentTo(xmlTextWriter);
            xmlTextWriter.Flush();
            memoryStream.Flush();
            memoryStream.Position = 0;
            using (var streamReader = new StreamReader(memoryStream))
            {
                content = streamReader.ReadToEnd();
            }
        }

        return content;
    }

    //=======================================================================================================
    /// <summary>
    /// Combines the header, sorted body blocks, and footer into one result.
    /// </summary>
    /// <param name="header">The lines that appear before the sortable blocks.</param>
    /// <param name="body">The blocks to sort and append to the result.</param>
    /// <param name="footer">The lines that appear after the sortable blocks.</param>
    /// <returns>An array containing the complete sorted file.</returns>
    //=======================================================================================================
    private static string[] Join(
        List<string> header,
        List<List<string>> body,
        List<string> footer)
    {
        body.Sort((list1, list2) =>
        {
            const string regex = @"((?<=public\s*static\s*string\s*)[^ ]*(?=\s*{)|(?<=name="")[^""]*)";

            var text1 = string.Join(Environment.NewLine, list1.ToArray());
            var text2 = string.Join(Environment.NewLine, list2.ToArray());

            const string assemblyTag = "<assembly";
            var isTagAssembly1 = text1.Contains(assemblyTag);
            var isTagAssembly2 = text2.Contains(assemblyTag);

            if (isTagAssembly1 && !isTagAssembly2) return -1;
            if (!isTagAssembly1 && isTagAssembly2) return 1;

            const string attributeForText = " xml:space=\"";
            var isNotText1 = !text1.Contains(attributeForText);
            var isNotText2 = !text2.Contains(attributeForText);

            if (isNotText1 && !isNotText2) return 1;
            if (!isNotText1 && isNotText2) return -1;

            var key1 = Regex.Match(text1, regex, RegexOptions.Singleline).Value;
            var key2 = Regex.Match(text2, regex, RegexOptions.Singleline).Value;

            return string.Compare(key1, key2, StringComparison.InvariantCulture);
        });
        foreach (var block in body) header.AddRange(block);
        header.AddRange(footer);
        return header.ToArray();
    }
}





