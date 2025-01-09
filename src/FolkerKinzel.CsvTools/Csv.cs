﻿using System.Text;

namespace FolkerKinzel.CsvTools;

/// <summary>Static class that provides methods for reading and writing CSV files.</summary>
public static class Csv
{
    /// <summary>The newline characters to use in CSV files ("\r\n").</summary>
    public const string NewLine = "\r\n";

    /// <summary> Analyzes the CSV file referenced by <paramref name="filePath" />.
    /// </summary>
    /// <param name="filePath">File path of the CSV file.</param>
    /// <param name="supposition">A supposition that is made about the presence of a header row.</param>
    /// <param name="textEncoding">
    /// The text encoding to be used to read the CSV file, or <c>null</c> to determine the <see cref="Encoding"/>
    /// automatically from the byte order mark (BOM).
    /// </param>
    /// <param name="analyzedLinesCount">Maximum number of lines to analyze in the CSV file. The minimum 
    /// value is <see cref="CsvAnalyzer.AnalyzedLinesMinCount" />. If the file has fewer lines than 
    /// <paramref name="analyzedLinesCount" />, it will be analyzed completely. (You can specify 
    /// <see cref="int.MaxValue">Int32.MaxValue</see> to analyze the entire file in any case.)</param>
    /// 
    /// <returns>The results of the analysis.</returns>
    /// 
    /// <remarks>
    /// <para>
    /// The method performs a statistical analysis on the CSV file to find the appropriate 
    /// parameters for reading the file. The result of the analysis is therefore always only an estimate, 
    /// the accuracy of which increases with the number of lines analyzed.
    /// </para>
    /// <para>
    /// The analysis is time-consuming because the CSV file has to be accessed for reading.
    /// </para>
    /// <para>
    /// This method also automatically determines the <see cref="Encoding"/> of the CSV file from the
    /// byte order mark (BOM) if the argument of the <paramref name="textEncoding"/> parameter is <c>null</c>.
    /// If the <see cref="Encoding"/> cannot be determined automatically, <see cref="Encoding.UTF8"/> is used.
    /// </para>
    /// </remarks>
    /// 
    /// <exception cref="ArgumentNullException"> <paramref name="filePath" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <para><paramref name="supposition"/> is not a defined value of 
    /// the <see cref="CsvSupposition"/> enum.</para>
    /// <para> - or -</para>
    /// <para><paramref name="supposition"/> is a combination of <see cref="CsvSupposition"/> values.</para>
    /// </exception>
    /// <exception cref="ArgumentException"> <paramref name="filePath" /> is not a valid
    /// file path.</exception>
    /// <exception cref="IOException">Error accessing the file.</exception>
    public static (Encoding, CsvAnalyzerResult) Analyze(string filePath,
                                                        CsvSupposition supposition = CsvSupposition.ProbablyHeaderPresent,
                                                        Encoding? textEncoding = null,
                                                        int analyzedLinesCount = CsvAnalyzer.AnalyzedLinesMinCount)
    {
        Encoding? encoding = textEncoding is null 
            ? TextEncodingConverter.TryGetEncoding(GetCodePage(filePath), out encoding)
                                  ? encoding
                                  : Encoding.UTF8
            : textEncoding;

        CsvAnalyzerResult results = CsvAnalyzer.Analyze(filePath, encoding, supposition, analyzedLinesCount);
        return (encoding, results);
    }

    /// <summary>This method analyzes the CSV file referenced by <paramref name="filePath" />
    /// first and then opens a <see cref="CsvReader"/> to read its content.
    /// </summary>
    /// <param name="filePath">File path of the CSV file.</param>
    /// <param name="supposition">A supposition that is made about the presence of a header row.</param>
    /// <param name="textEncoding">
    /// The text encoding to be used to read the CSV file, or <c>null</c> to determine the <see cref="Encoding"/>
    /// automatically from the byte order mark (BOM).
    /// </param>
    /// <param name="analyzedLines">Maximum number of lines to analyze in the CSV file. The minimum 
    /// value is <see cref="CsvAnalyzer.AnalyzedLinesMinCount" />. If the file has fewer lines than 
    /// <paramref name="analyzedLines" />, it will be analyzed completely. (You can specify 
    /// <see cref="int.MaxValue">Int32.MaxValue</see> to analyze the entire file in any case.)</param>
    /// <param name="disableCaching"><c>true</c> to set the <see cref="CsvOpts.DisableCaching"/> flag, 
    /// otherwise <c>false</c>.</param>
    /// 
    /// <returns>A <see cref="CsvReader"/> that allows to iterate the data.</returns>
    /// 
    /// <remarks>
    /// <para>
    /// The method performs a statistical analysis on the CSV file to find the appropriate 
    /// parameters for reading the file. The result of the analysis is therefore always only an estimate, 
    /// the accuracy of which increases with the number of lines analyzed.
    /// </para>
    /// <para>
    /// The analysis is time-consuming because the CSV file has to be accessed for reading.
    /// </para>
    /// <para>
    /// This method also automatically determines the <see cref="Encoding"/> of the CSV file from the
    /// byte order mark (BOM) if the argument of the <paramref name="textEncoding"/> parameter is <c>null</c>.
    /// If the <see cref="Encoding"/> cannot be determined automatically, <see cref="Encoding.UTF8"/> is used.
    /// </para>
    /// </remarks>
    /// 
    /// <example>
    /// <note type="note">
    /// In the following code examples - for easier readability - exception handling
    /// has been omitted.
    /// </note>
    /// 
    /// <code language="cs" source="..\..\..\FolkerKinzel.CsvTools\src\Examples\CsvExample.cs" />
    /// </example>
    /// 
    /// <exception cref="ArgumentNullException"> <paramref name="filePath" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <para><paramref name="supposition"/> is not a defined value of 
    /// the <see cref="CsvSupposition"/> enum.</para>
    /// <para> - or -</para>
    /// <para><paramref name="supposition"/> is a combination of <see cref="CsvSupposition"/> values.</para>
    /// </exception>
    /// <exception cref="ArgumentException"> <paramref name="filePath" /> is not a valid
    /// file path.</exception>
    /// <exception cref="IOException">Error accessing the file.</exception>
    public static CsvReader OpenReadAnalyzed(string filePath,
                                             CsvSupposition supposition = CsvSupposition.ProbablyHeaderPresent,
                                             Encoding? textEncoding = null,
                                             int analyzedLines = CsvAnalyzer.AnalyzedLinesMinCount,
                                             bool disableCaching = false)
    {
        (Encoding encoding, CsvAnalyzerResult result) = Analyze(filePath, supposition, textEncoding, analyzedLines);
        result.Options = disableCaching ? result.Options | CsvOpts.DisableCaching : result.Options;
        return result.HeaderPresent 
            ? new(filePath, headerPresent: true, result.Options, result.Delimiter, encoding)
            : new(filePath, result, encoding);
    }

    /// <summary>Opens the CSV file referenced with <paramref name="filePath"/> for reading.</summary>
    /// <param name="filePath">File path of the CSV file to read.</param>
    /// <param name="headerPresent"> <c>true</c>, if the CSV file has a header with column
    /// names.</param>
    /// <param name="options">Options for reading the CSV file.</param>
    /// <param name="delimiter">The field separator character.</param>
    /// <param name="textEncoding">The text encoding to be used to read the CSV file
    /// or <c>null</c> for <see cref="Encoding.UTF8" />.</param>
    /// 
    /// <returns>A <see cref="CsvReader"/> that allows you to iterate through the CSV data.</returns>
    /// 
    /// <remarks>
    /// <note type="tip">
    /// The optimal parameters can be determined automatically with <see cref="CsvAnalyzer"/> - or use
    /// <see cref="OpenReadAnalyzed(string, CsvSupposition, Encoding?, int, bool)"/>.
    /// </note>
    /// </remarks>
    /// 
    /// <exception cref="ArgumentNullException"> <paramref name="filePath" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"> <paramref name="filePath" /> is not a valid
    /// file path.</exception>
    /// <exception cref="IOException">Error accessing the disk.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CsvReader OpenRead(string filePath,
                                     bool headerPresent = true,
                                     CsvOpts options = CsvOpts.Default,
                                     char delimiter = ',',
                                     Encoding? textEncoding = null)
        => new(filePath, headerPresent, options, delimiter, textEncoding);


    /// <summary>Initializes a <see cref="CsvReader"/> instance to read data that is in the 
    /// CSV format.</summary>
    /// <param name="reader">The <see cref="TextReader" /> with which the CSV data is
    /// read.</param>
    /// <param name="headerPresent"> <c>true</c>, if the CSV data has a header with column
    /// names, otherwise <c>false</c>.</param>
    /// <param name="options">Options for reading CSV.</param>
    /// <param name="delimiter">The field separator character.</param>
    /// 
    /// <returns>A <see cref="CsvReader"/> that allows you to iterate through the CSV data.</returns>
    /// 
    /// <exception cref="ArgumentNullException"> <paramref name="reader" /> is <c>null</c>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CsvReader OpenRead(TextReader reader,
                                     bool headerPresent = true,
                                     CsvOpts options = CsvOpts.Default,
                                     char delimiter = ',')
        => new(reader, headerPresent, options, delimiter);


    /// <summary>Creates a new CSV file with header row and initializes a <see cref="CsvWriter"/> instance
    /// to write data to it. If the target file already exists, it is truncated and overwritten.</summary>
    /// <param name="filePath">The file path of the CSV file to be written.</param>
    /// <param name="columnNames">A collection of column names for the header to be written.
    /// The collection will be copied. If the collection contains <c>null</c> values, these 
    /// are replaced by automatically generated column names. Column names cannot appear twice. 
    /// With <paramref name="caseSensitive"/> can be chosen whether the comparison is case-sensitive or not.</param>
    ///  <param name="caseSensitive">If <c>true</c>, column names that differ only in 
    /// upper and lower case are also accepted, otherwise <c>false</c>.</param>
    /// <param name="textEncoding">The text encoding to be used or <c>null</c> for <see
    /// cref="Encoding.UTF8" />.</param>
    /// 
    /// <returns>A <see cref="CsvWriter"/> instance that allows you to write data as a CSV file.</returns>
    /// 
    /// <remarks>
    /// This method initializes a <see cref="CsvWriter"/> instance that uses the comma ',' (%x2C) as field delimiter.
    /// This complies with the RFC 4180 standard. If another delimiter is required, use the constructor of
    /// <see cref="CsvWriter"/> directly."/>
    /// </remarks>
    /// 
    /// <exception cref="ArgumentNullException"> <paramref name="filePath" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">
    /// <para>
    /// <paramref name="filePath" /> is not a valid file path
    /// </para>
    /// <para>
    /// - or -
    /// </para>
    /// <para>
    /// a column name in <paramref name="columnNames" /> occurs twice. With <paramref name="caseSensitive"/>
    /// can be chosen whether the comparison is case-sensitive or not.
    /// </para>
    /// </exception>
    /// <exception cref="IOException">I/O-Error</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CsvWriter OpenWrite(string filePath,
                                      IEnumerable<string?> columnNames,
                                      bool caseSensitive = false,
                                      Encoding? textEncoding = null)
        => new(filePath, columnNames, caseSensitive, textEncoding);

    /// <summary>
    /// Initializes a new <see cref="CsvWriter" /> object with the column names
    /// for the header row to be written.</summary>
    /// <param name="writer">The <see cref="TextWriter" /> used for writing.</param>
    /// <param name="columnNames">A collection of column names for the header to be written.
    /// The collection will be copied. If the collection contains <c>null</c> values, these 
    /// are replaced with automatically
    /// generated column names. Column names cannot appear twice. With <paramref name="caseSensitive"/>
    /// can be chosen whether the comparison is case-sensitive or not.</param>
    /// <param name="caseSensitive">If <c>true</c>, column names that differ only in 
    /// upper and lower case are also accepted, otherwise <c>false</c>.</param>
    /// 
    /// <returns>A <see cref="CsvWriter" /> instance that allows you to write CSV data with
    /// <paramref name="writer"/>.</returns>
    /// 
    /// <remarks>
    /// This method initializes a <see cref="CsvWriter"/> instance that uses the comma ',' (%x2C) as field delimiter.
    /// This complies with the RFC 4180 standard. If another delimiter is required, use the constructor of
    /// <see cref="CsvWriter"/> directly."/>
    /// </remarks>
    /// 
    /// <exception cref="ArgumentNullException"> <paramref name="writer" /> or <paramref
    /// name="columnNames" /> is <c>null.</c></exception>
    /// <exception cref="ArgumentException">A column name in <paramref name="columnNames"
    /// /> occurs twice. With <paramref name="caseSensitive"/> can be chosen whether 
    /// the comparison is case-sensitive or not.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CsvWriter OpenWrite(TextWriter writer,
                                      IEnumerable<string?> columnNames,
                                      bool caseSensitive = false)
        => new(writer, columnNames, caseSensitive);

    /// <summary>Creates a new CSV file without a header row and initializes a <see cref="CsvWriter"/> 
    /// instance to write data to it. If the target file already exists, it is truncated and overwritten.</summary>
    /// <param name="filePath">The file path of the CSV file to be written.</param>
    /// <param name="columnsCount">Number of columns in the CSV file.</param>
    /// <param name="textEncoding">The text encoding to be used or <c>null</c> for <see
    /// cref="Encoding.UTF8" />.</param>
    /// 
    /// <returns>A <see cref="CsvWriter"/> instance that allows you to write data as a CSV file.</returns>
    /// 
    /// <remarks>
    /// This method initializes a <see cref="CsvWriter"/> instance that uses the comma ',' (%x2C) as field delimiter.
    /// This complies with the RFC 4180 standard. If another delimiter is required, use the constructor of
    /// <see cref="CsvWriter"/> directly."/>
    /// </remarks>
    /// 
    /// <exception cref="ArgumentNullException"> <paramref name="filePath" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"> <paramref name="filePath" /> is not a valid
    /// file path.</exception>
    /// <exception cref="IOException">I/O-Error</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CsvWriter OpenWrite(string filePath,
                                      int columnsCount,
                                      Encoding? textEncoding = null)
        => new(filePath, columnsCount, textEncoding);

    /// <summary>Initializes a new <see cref="CsvWriter" /> object to write CSV data
    /// without a header row.</summary>
    /// <param name="writer">The <see cref="TextWriter" /> used for writing.</param>
    /// <param name="columnsCount">Number of columns in the CSV.</param>
    /// 
    /// <returns>A <see cref="CsvWriter" /> instance that allows you to write CSV data with
    /// the <see cref="TextWriter"/>.</returns>
    /// 
    /// <remarks>
    /// This method initializes a <see cref="CsvWriter"/> instance that uses the comma ',' (%x2C) as field delimiter.
    /// This complies with the RFC 4180 standard. If another delimiter is required, use the constructor of
    /// <see cref="CsvWriter"/> directly."/>
    /// </remarks>
    /// 
    /// <exception cref="ArgumentNullException"> <paramref name="writer" /> is <c>null.</c></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CsvWriter OpenWrite(TextWriter writer, int columnsCount)
        => new(writer, columnsCount);

    ///// <summary>Writes <paramref name="data"/> to a new CSV file with a header row. If the 
    ///// target file already exists, it is truncated and overwritten.</summary>
    ///// <param name="data">The data to be written to the CSV file.</param>
    ///// <param name="filePath">The file path of the CSV file to be written.</param>
    ///// <param name="columnNames">A collection of column names for the header to be written.
    ///// The collection will be copied. If the collection contains <c>null</c> values, these 
    ///// are replaced by automatically generated column names. Column names cannot appear twice. 
    ///// It is to note that the comparison is not case-sensitive - unless this option is explicitely
    ///// chosen in <paramref name="options" />.</param>
    ///// <param name="options">Options for the CSV file to be written.</param>
    ///// <param name="textEncoding">The text encoding to be used or <c>null</c> for <see
    ///// cref="Encoding.UTF8" />.</param>
    ///// <param name="delimiter">The field separator character.</param>
    ///// 
    ///// <example>
    ///// <note type="note">
    ///// In the following code examples - for easier readability - exception handling
    ///// has been omitted.
    ///// </note>
    ///// 
    ///// <code language="cs" source="..\..\..\FolkerKinzel.CsvTools\src\Examples\CsvExample.cs" />
    ///// </example>
    ///// 
    ///// <exception cref="ArgumentNullException"> <paramref name="filePath" />, or <paramref name="data" />, 
    ///// or one of its items is <c>null</c>.</exception>
    ///// <exception cref="ArgumentOutOfRangeException"> An item of <paramref name="data" /> contains
    ///// more items than <paramref name="columnNames"/> />.</exception>
    ///// <exception cref="ArgumentException">
    ///// <para>
    ///// <paramref name="filePath" /> is not a valid file path
    ///// </para>
    ///// <para>
    ///// - or -
    ///// </para>
    ///// <para>
    ///// a column name in <paramref name="columnNames" /> occurs twice. In <paramref
    ///// name="options" /> you can choose, whether the comparison of column names is
    ///// case-sensitive.
    ///// </para>
    ///// </exception>
    ///// <exception cref="IOException">I/O-Error</exception>
    ///// <exception cref="ObjectDisposedException">The file was already closed.</exception>
    //public static void Write(IEnumerable<IEnumerable<ReadOnlyMemory<char>>> data,
    //                         string filePath,
    //                         IEnumerable<string?> columnNames,
    //                         CsvOpts options = CsvOpts.Default,
    //                         Encoding? textEncoding = null,
    //                         char delimiter = ',')
    //{
    //    using CsvWriter writer = new(filePath, columnNames, options, textEncoding, delimiter);
    //    DoWrite(writer, data);
    //}

    ///// <summary>Writes <paramref name="data"/> in CSV format with a header row.</summary>
    ///// <param name="data">The data to be written in CSV format.</param>
    ///// <param name="writer">The <see cref="TextWriter" /> used for writing.</param>
    ///// <param name="columnNames">A collection of column names for the header to be written.
    ///// The collection will be copied. If the collection contains <c>null</c> values, these 
    ///// are replaced with automatically
    ///// generated column names. Column names cannot appear twice. It is to note that
    ///// the comparison is not case-sensitive - unless this option is explicitely chosen
    ///// in <paramref name="options" />.</param>
    ///// <param name="options">Options for the CSV.</param>
    ///// <param name="delimiter">The field separator character.</param>
    ///// 
    ///// <exception cref="ArgumentNullException"> <paramref name="writer" />, or <paramref name="columnNames"/>,
    ///// or <paramref name="data" />, or one of its items is <c>null</c>.</exception>
    ///// <exception cref="ArgumentOutOfRangeException"> An item of <paramref name="data" /> contains
    ///// more items than <paramref name="columnNames"/> />.</exception>
    /////  <exception cref="ArgumentException">A column name in <paramref name="columnNames"
    ///// /> occurs twice. In <paramref name="options" /> can be chosen whether the comparison
    ///// is case-sensitive.</exception>
    ///// <exception cref="IOException">I/O-Error</exception>
    ///// <exception cref="ObjectDisposedException"><paramref name="writer"/> was already closed.</exception>
    //public static void Write(IEnumerable<IEnumerable<ReadOnlyMemory<char>>> data,
    //                         TextWriter writer,
    //                         IEnumerable<string?> columnNames,
    //                         CsvOpts options = CsvOpts.Default,
    //                         char delimiter = ',')
    //{
    //    using CsvWriter csvWriter = new(writer, columnNames, options, delimiter);
    //    DoWrite(csvWriter, data);
    //}

    ///// <summary>Writes <paramref name="data"/> to a new CSV file without a header row. If the 
    ///// target file already exists, it is truncated and overwritten.</summary>
    ///// <param name="data">The data to be written to the CSV file.</param>
    ///// <param name="filePath">The file path of the CSV file to be written.</param>
    ///// <param name="columnsCount">Number of columns in the CSV file.</param>
    ///// <param name="options">Options for the CSV file to be written.</param>
    ///// <param name="textEncoding">The text encoding to be used or <c>null</c> for <see
    ///// cref="Encoding.UTF8" />.</param>
    ///// <param name="delimiter">The field separator character.</param>
    ///// 
    ///// <exception cref="ArgumentNullException"> <paramref name="filePath" />, or <paramref name="data" />, 
    ///// or one of its items is <c>null</c>.</exception>
    ///// <exception cref="ArgumentOutOfRangeException"> An item of <paramref name="data" /> contains
    ///// more items than <paramref name="columnsCount"/> />.</exception>
    ///// <exception cref="ArgumentException"> <paramref name="filePath" /> is not a valid
    ///// file path.</exception>
    ///// <exception cref="IOException">I/O-Error</exception>
    ///// <exception cref="ObjectDisposedException">The file was already closed.</exception>
    //public static void Write(IEnumerable<IEnumerable<ReadOnlyMemory<char>>> data,
    //                         string filePath,
    //                         int columnsCount,
    //                         CsvOpts options = CsvOpts.Default,
    //                         Encoding? textEncoding = null,
    //                         char delimiter = ',')
    //{
    //    using CsvWriter writer = new(filePath, columnsCount, options, textEncoding, delimiter);
    //    DoWrite(writer, data);
    //}

    ///// <summary>Writes <paramref name="data"/> in CSV format without a header row.</summary>
    ///// <param name="data">The data to be written in CSV format.</param>
    ///// <param name="writer">The <see cref="TextWriter" /> used for writing.</param>
    ///// <param name="columnsCount">Number of columns in the CSV.</param>
    ///// <param name="options">Options for the CSV.</param>
    ///// <param name="delimiter">The field separator character.</param>
    ///// 
    ///// <exception cref="ArgumentNullException"> <paramref name="writer" />, or <paramref name="data" />, or one of its items
    ///// is <c>null</c>.</exception>
    ///// <exception cref="ArgumentOutOfRangeException"> An item of <paramref name="data" /> contains
    ///// more items than <paramref name="columnsCount"/> />.</exception>
    ///// <exception cref="IOException">I/O-Error</exception>
    ///// <exception cref="ObjectDisposedException"><paramref name="writer"/> was already closed.</exception>
    //public static void Write(IEnumerable<IEnumerable<ReadOnlyMemory<char>>> data,
    //                         TextWriter writer,
    //                         int columnsCount,
    //                         CsvOpts options = CsvOpts.Default,
    //                         char delimiter = ',')
    //{
    //    using CsvWriter csvWriter = new(writer, columnsCount, options, delimiter);
    //    DoWrite(csvWriter, data);
    //}

    private static int GetCodePage(string filePath)
    {
        const int BUF_LENGTH = 4;

        try
        {
            using FileStream fs = File.OpenRead(filePath);

#if NET8_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            Span<byte> span = stackalloc byte[BUF_LENGTH];
            int bytesRead = fs.Read(span);
#else
            var buf = new byte[BUF_LENGTH];
            int bytesRead = fs.Read(buf, 0, buf.Length);
            ReadOnlySpan<byte> span = buf;
#endif
            return TextEncodingConverter.GetCodePage(span.Slice(0, bytesRead), out _);
        }
        catch
        {
            return Encoding.UTF8.CodePage;
        }
    }

    //private static void DoWrite(CsvWriter writer, IEnumerable<IEnumerable<ReadOnlyMemory<char>>> values)
    //{
    //    _ArgumentNullException.ThrowIfNull(values, nameof(values));

    //    foreach (IEnumerable<ReadOnlyMemory<char>> record in values)
    //    {
    //        writer.Record.Fill(record);
    //        writer.Write();
    //    }
    //}
}
