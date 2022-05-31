﻿/*
    ============================================================================

    Module Name:        Program.cs

    Namespace Name:     FixCSV

    Class Name:         Program

    Synopsis:           This command line utility fixes up a CSV file that
                        conforms to the 

    Remarks:            This class module implements the Program class, which is
                        composed of the static void Main method, functionally
                        equivalent to the main() routine of a standard C program.

                        The objective of this character-mode program is to
                        transform a CSV file that adheres to RFC 4180, but has
                        some fields that span multiple lines, and is, therefore,
                        unsuitable for use with most standard CSV import
                        routines.

    Reference:          RFC 4180
                        http://www.faqs.org/rfcs/rfc4180.html

    Author:             David A. Gray

    ----------------------------------------------------------------------------
    Revision History
    ----------------------------------------------------------------------------

    Date       Version Author Synopsis
    ---------- ------- ------ -------------------------------------------------
    2020/09/07 1.0     DAGray Initial implementation.

    2020/10/12 1.1     DAGray 1) Add a help message when the name of the input
                                 file is omitted.

                              2) Fix a bug exposed by attempting to process a
                                 CSV generated by ZOOMINFO.

                              3) Compensate for a quirk seen in the ZOOMINFO
                                 export, and elsewhere from time to time. The
                                 quirk is that the file contains a quirky double
                                 line break, 0d 0d 0a. That's a legacy Macintosh
                                 line break, followed by a Windows line break.

    2021/01/02 1.2     DAGray Display the initial and final line counts.

    2022/05/31 1.3     DAGray Fix a MissingMethodException raised by the Trace
                              Listener subsystem. This fix is accompanied by a
                              minor update of the WizardWrx .NET API packages
                              that are included herein.
    ============================================================================
*/

using System;
using System.IO;
using System.Text;

using WizardWrx;

using WizardWrx.ConsoleAppAids3;
using WizardWrx.Core;
using WizardWrx.DLLConfigurationManager;


namespace FixCSV
{
    class Program
    {
        const int ERR_RUNTIME = 1;
        const int ERR_FILENAME_MISSING = ERR_RUNTIME + 1;
        const int ERR_FNF = ERR_FILENAME_MISSING + 1;
        const int WRM_COLLISION = ERR_FNF + 1;


        private struct OperatingParameters
        {
            public string InputFileNamePerCmd;
            public string BackupFileName;
            public string NewlineReplacementToken;
        };  // private struct OperatingParameters


        private static readonly string [ ] s_astrNewlineTokenList = new string [ ]
        {
                SpecialStrings.STRING_SPLIT_NEWLINE ,
                SpecialStrings.STRING_SPLIT_LINEFEED ,
                SpecialStrings.STRING_SPLIT_CARRIAGE_RETURN
        };  // private static readonly string [ ] s_astrNewlineTokenList

        //  --------------------------------------------------------------------
        //  This string represents a quirky newline, 0d 0d 0a (\r\r\n).
        //  --------------------------------------------------------------------

        private static readonly string s_strQuirkyNewline = string.Concat (
            SpecialStrings.STRING_SPLIT_CARRIAGE_RETURN ,
            Environment.NewLine );

        private static ConsoleAppStateManager s_theApp = ConsoleAppStateManager.GetTheSingleInstance ( );
        private static ExceptionLogger s_exLogger = s_theApp.BaseStateManager.AppExceptionLogger;
        private static OperatingParameters s_operatingParameters;


        static void Main ( string [ ] args )
        {
            s_exLogger.OptionFlags = s_exLogger.OptionFlags
                                     | ExceptionLogger.OutputOptions.Stack
                                     | ExceptionLogger.OutputOptions.EventLog;
            s_theApp.BaseStateManager.LoadErrorMessageTable (
                new string [ ]
                {
                    null ,														// ERROR_SUCCESS
                    Properties.Resources.MSG_ERROR_RUNTIME_EXCEPTION ,			// ERR_RUNTIME
                    Properties.Resources.MSG_ERROR_FILE_TO_PROCESS_OMITTED ,	// ERR_FILENAME_MISSING
                    Properties.Resources.MSG_ERROR_FILE_TO_PROCESS_NOT_FOUND ,	// ERR_FNF
                    Properties.Resources.MSG_ERROR_BACKUP_OF_BACKUP				// WRM_NAME_COLLISION
                } );
            s_theApp.DisplayBOJMessage ( );
            CmdLneArgsBasic cmdLneArgs = new CmdLneArgsBasic (
                new string [ ]
                {
                    Properties.Resources.CMD_ARG_INPUT_FILENAME,
                    Properties.Resources.CMD_ARG_NEWLINE_REPL_TOKEN
                } );
            s_operatingParameters.InputFileNamePerCmd = cmdLneArgs.GetArgByName (
                Properties.Resources.CMD_ARG_INPUT_FILENAME );

            //  ----------------------------------------------------------------
            //  Evaluate the sole required command line argument. Unless it has
            //  a value, there is no point proceeding with anything more.
            //  ----------------------------------------------------------------

            if ( !string.IsNullOrEmpty ( s_operatingParameters.InputFileNamePerCmd ) )
            {
                s_operatingParameters.NewlineReplacementToken = SetEmbeddedNewlineReplacementValue ( cmdLneArgs );
                s_operatingParameters.BackupFileName = string.Concat (
                    s_operatingParameters.InputFileNamePerCmd ,
                    Properties.Resources.MSG_BACKUP_FILE_EXTENSION );
                Console.WriteLine (
                    Properties.Resources.MSG_INFO_INPUT_FILENAME ,              // Format control string
                    s_operatingParameters.InputFileNamePerCmd ,                 // Name of event log export file to process
                    s_operatingParameters.BackupFileName ,                      // Name of backup file to create
                    Environment.NewLine );                                      // Embedded newline

                //  ------------------------------------------------------------
                //  From here to almost the end involves interacting with the
                //  file system, so the majority of the main routine runs inside
                //  a try/catch block.
                //  ------------------------------------------------------------

                try
                {
                    if ( File.Exists ( s_operatingParameters.InputFileNamePerCmd ) )
                    {
                        if ( MakeBackup ( s_operatingParameters.InputFileNamePerCmd , s_operatingParameters.BackupFileName ) )
                        {
                            ProcessInputFile ( );
                        }   // if ( MakeBackup ( s_operatingParameters.InputFileNamePerCmd , s_operatingParameters.BackupFileName ) )
                    }   // TRUE (anticipated outcome) block, if ( File.Exists ( s_operatingParameters.InputFileNamePerCmd ) )
                    else
                    {
                        s_theApp.BaseStateManager.AppReturnCode = ERR_FNF;
                    }   // FALSE (unanticipated outcome) block, if ( File.Exists ( s_operatingParameters.InputFileNamePerCmd ) )
                }
                catch ( Exception exAll )
                {
                    s_theApp.BaseStateManager.AppExceptionLogger.ReportException ( exAll );
                    s_theApp.BaseStateManager.AppReturnCode = ERR_RUNTIME;
                }
            }   // TRUE (anticipated outcome) block, if ( !string.IsNullOrEmpty ( s_operatingParameters.InputFileNamePerCmd ) )
            else
            {
                s_theApp.BaseStateManager.AppReturnCode = ERR_FILENAME_MISSING;
                Console.WriteLine (
                    Properties.Resources.MSG_SYNTAX_HELP ,                      // Format Control String, presently "{1}Minimum command line:{1}{1}{0} InputFileName=CSVFileName{1}where CSVFileName = Name of CSV to process{1}"
                    s_theApp.BaseStateManager.AppRootAssemblyFileName ,         // Format Token 0: {0} InputFileName=CSVFileName
                    Environment.NewLine );                                      // Format Token 1: Newlines everywhere else
            }   // FALSE (unanticipated outcome) block, if ( !string.IsNullOrEmpty ( s_operatingParameters.InputFileNamePerCmd ) )

            //  ----------------------------------------------------------------
            //  Summarize and wrap up takes one of two turns, depending on the
            //  value of the return code stored in the application manager.
            //  ----------------------------------------------------------------

            if ( s_theApp.BaseStateManager.AppReturnCode == MagicNumbers.ERROR_SUCCESS )
            {
                s_theApp.NormalExit ( ConsoleAppStateManager.NormalExitAction.Timed );
            }   // TRUE (desired outcome) block, if ( s_theApp.BaseStateManager.AppReturnCode == MagicNumbers.ERROR_SUCCESS )
            else
            {   // Strictly speaking, one of the outcomes is just a warning.
                Console.WriteLine ( );
                s_theApp.ErrorExit ( ( uint ) s_theApp.BaseStateManager.AppReturnCode );
            }	// false (undesired outcome) block, if ( s_theApp.BaseStateManager.AppReturnCode == MagicNumbers.ERROR_SUCCESS )
        }   // static void Main


        /// <summary>
        /// Segregate the task of correcting the quirky line breaks seen from
        /// time to time in CSV files exported by all kinds of applications.
        /// </summary>
        /// <param name="psb">
        /// Pass in a reference to the StringBuilder in which the calling core
        /// processing routine assembles the RFC 4180 compliant CSV dataset.
        /// </param>
        /// <returns>
        /// The return value is the difference between the initial length of the
        /// string and its final length. Since the quirky line breaks have three
        /// characters, while its replacement has only two, the difference is a
        /// proxy for the number of quirks found and replaced.
        /// </returns>
        private static int FindAndFixLimeBreakQuirks ( StringBuilder psb )
        {   // 0d 0d 0a
            int intOriginalCharacterCount = psb.Length;
            psb.Replace (
                s_strQuirkyNewline ,
                Environment.NewLine );
            int intNewCharacterCount = psb.Length;

            return intOriginalCharacterCount - intNewCharacterCount;
        }   // private static void FindAndFixLimeBreakQuirks


        /// <summary>
        /// Create a backup of the input file by file copy. If a like named file
        /// exists, first rename it, liberating its name for reuse.
        /// </summary>
        /// <param name="pstrInputFileName">
        /// By design, the input file exists when this routine get the call.
        /// </param>
        /// <param name="pstrBackupFileName">
        /// Depending on how the input file came into being, there may or may
        /// not be a backup file.
        /// </param>
        /// <returns>
        /// Unless a system error prevents making a backup, this method returns
        /// TRUE, and the input file is parsed. Otherwise, the return value is
        /// FALSE, and further processing immediately stops.
        /// </returns>
        /// <remarks>
        /// In the unlikely, though certainly possible, event of a falure, this
        /// method sets s_theApp.BaseStateManager.AppReturnCode to a nonzero
        /// value, which the wrap-up routine detects and handles.
        /// </remarks>
        private static bool MakeBackup (
            string pstrInputFileName ,
            string pstrBackupFileName )
        {
            const string NEWNAME_TEMPLATE = @"{0}_{1}";

            try
            {
                if ( File.Exists ( pstrBackupFileName ) )
                {
                    int intNameCounter = MagicNumbers.PLUS_ONE;
                    string strProposedName = string.Format (
                        NEWNAME_TEMPLATE ,                                      // The format control string is a simple filename template that appends a sequential suffix to the base name.
                        pstrBackupFileName ,                                    // The original backup filename begins all proposed names, so that they stay together in a directory listing sorted by file name.
                        intNameCounter );                                       // Since the counter is initialized to 1, the first proposed name has "_1" appended to the original backup name.

                    while ( File.Exists ( strProposedName ) )
                    {   // Increment the counter, and go around.
                        intNameCounter++;
                    }   // while ( File.Exists ( strProposedName ) )

                    File.Move (
                        pstrBackupFileName ,                                    // When execution reaches this point, the name in strProposedName is available.
                        strProposedName );                                      // Like the Windows API before it, the System.IO.File object treats renaming as moving.

                    Console.WriteLine (
                        Properties.Resources.MSG_WARNING_BACKUP_FILE_EXISTS ,   // Format Control String
                        pstrBackupFileName ,                                    // Intended name of backup file
                        strProposedName ,                                       // Name assigned to existing backup file to clear the way for using pstrBackupFileName
                        Environment.NewLine );                                  // Embedded Newline
                    s_theApp.BaseStateManager.AppReturnCode = WRM_COLLISION;    // Subject to change in the event of a runtime error, a status code signals the duplicate file name.
                }   // if ( File.Exists ( pstrBackupFileName ) )

                File.Move (
                    pstrInputFileName ,                                         // Name of Original
                    pstrBackupFileName );                                       // Designated name of backup
                return true;
            }
            catch ( Exception exAll )
            {
                s_theApp.BaseStateManager.AppExceptionLogger.ReportException ( exAll );
                s_theApp.BaseStateManager.AppReturnCode = ERR_RUNTIME;
                return false;
            }
        }	// private static bool MakeBackup


        /// <summary>
        /// Clean the guarded string by replacing newlines with the token
        /// specified in the command line.
        /// </summary>
        /// <param name="pstrGuarded">
        /// Pass in the substring that contains the current guarded string.
        /// </param>
        /// <param name="pastrNewlineTokenList">
        /// Pass in the list of newline tokens in the order in which they must
        /// be processed to prevent inserting extra newlines.
        /// </param>
        /// <returns>
        /// The return value is the cleaned string.
        /// </returns>
        private static string CleanGuardedString (
            string pstrGuarded ,
            string [ ] pastrNewlineTokenList )
        {
            StringBuilder builder = new StringBuilder (
                pstrGuarded ,
                pstrGuarded.Length );

            int intNReplacementStrings = pastrNewlineTokenList.Length;

            for ( int intCurrentReplacementString = ArrayInfo.ARRAY_FIRST_ELEMENT ;
                intCurrentReplacementString < intNReplacementStrings ;
                intCurrentReplacementString++ )
            {
                builder.Replace (
                    pastrNewlineTokenList [ intCurrentReplacementString ] ,
                    s_operatingParameters.NewlineReplacementToken );
            }   // for ( int intCurrentReplacementString = ArrayInfo.ARRAY_FIRST_ELEMENT ; intCurrentReplacementString < intNReplacementStrings ; intCurrentReplacementString++ )

            if ( pstrGuarded.Length != builder.Length )
            {
                TraceLogger.WriteWithLabeledLocalTime (
                    string.Format (
                        @"In CleanGuardedString, pstrGuarded.Length = {0}, builder.Length = {1}" ,     // Format Control String
                        pstrGuarded.Length ,                                    // Format Item 0: pstrGuarded.Length = {0}
                        builder.Length ) );                                     // Format Item 1: builder.Length = {1}
            }   // if ( pstrGuarded.Length != builder.Length )

            return builder.ToString ( );
        }   // private static string CleanGuardedString


        /// <summary>
        /// Perform the core processing that is the mission of this program.
        /// </summary>
        /// <remarks>
        /// Everything this routine needs, including its error reporting
        /// mechanism, is the two static objects owned by the class.
        /// </remarks>
        private static void ProcessInputFile ( )
        {
            const char GUARD_CHARACTER = SpecialCharacters.DOUBLE_QUOTE;

            FileInfo outFileInfo = new FileInfo ( s_operatingParameters.InputFileNamePerCmd );
            FileInfo inFileInfo = new FileInfo ( s_operatingParameters.BackupFileName );

            Console.WriteLine (
                inFileInfo.ShowFileDetails (
                    FileInfoExtensionMethods.FileDetailsToShow.Everything ,
                    Properties.Resources.MSG_INPUT_FILE_DETAILS ) );

            if ( outFileInfo.Exists )
            {
                outFileInfo.FileAttributeClear ( FileAttributes.ReadOnly );
                Console.WriteLine (
                    outFileInfo.ShowFileDetails (
                        FileInfoExtensionMethods.FileDetailsToShow.Everything ,
                        Properties.Resources.MSG_OUTPUT_FILE_DETAILS ) );
            }   // TRUE block, if ( outFileInfo.Exists )
            else
            {
                Console.WriteLine ( Properties.Resources.MSG_OUTPUT_FILE_IS_NEW );
            }   // FALSE block, if ( outFileInfo.Exists )

            //  ----------------------------------------------------------------
            //  The real work begins here.
            //  ----------------------------------------------------------------

            string strInputFileContent = File.ReadAllText ( inFileInfo.FullName );

            int intInputCRCount = strInputFileContent.CountCharacterOccurrences ( SpecialCharacters.CARRIAGE_RETURN );
            int intInputLFCount = strInputFileContent.CountCharacterOccurrences ( SpecialCharacters.LINEFEED );

#pragma warning disable IDE0059 // Unnecessary assignment of a value
            int intNChars2Append = ListInfo.EMPTY_STRING_LENGTH;
            int intNInputChars = strInputFileContent.Length;
            int intPosCurrent = ListInfo.BEGINNING_OF_BUFFER;
            int intPosLeftGuardChar = ListInfo.INDEXOF_NOT_FOUND;
            int intPosRightGuardChar = ListInfo.INDEXOF_NOT_FOUND;
#pragma warning restore IDE0059 // Unnecessary assignment of a value

            bool fMoreChars2Process = true;
            bool fInsideGuardedBlock = false;
            bool fLastGuardAccountedFor = false;

            StringBuilder builder = new StringBuilder ( intNInputChars );

            while ( fMoreChars2Process )
            {
                if ( fInsideGuardedBlock )
                {
                    intPosRightGuardChar = ScanForNextGuardCharacter (
                        GUARD_CHARACTER ,
                        strInputFileContent ,
                        intPosCurrent );

                    if ( intPosRightGuardChar > ListInfo.INDEXOF_NOT_FOUND )
                    {
                        intNChars2Append = (   intPosRightGuardChar
                                             - intPosCurrent )
                                           + MagicNumbers.PLUS_ONE;
                        string strGuardedAndClean = CleanGuardedString (
                            strInputFileContent.Substring (
                                intPosCurrent ,
                                intNChars2Append ) ,
                            s_astrNewlineTokenList );
                        builder.Append ( strGuardedAndClean );
                        fInsideGuardedBlock = false;
                    }   // TRUE (anticipated outcome) block, if ( intPosRightGuardChar > ListInfo.INDEXOF_NOT_FOUND )
                    else
                    {
                        throw new Exception (
                            string.Format (
                                Properties.Resources.ERRMSG_MISSING_RIGHT_GUARD ,
                                intPosLeftGuardChar ,
                                intPosCurrent ,
                                intNInputChars ,
                                Environment.NewLine ) );
                    }   // FALSE (unanticipated outcome) block, if ( intPosRightGuardChar > ListInfo.INDEXOF_NOT_FOUND )
                }   // TRUE (intPosCurrent is WITHIN a guarded block.) block, if ( fInsideGuardedBlock )
                else
                {
                    intPosLeftGuardChar = ScanForNextGuardCharacter (
                        GUARD_CHARACTER ,
                        strInputFileContent ,
                        intPosCurrent );

                    if ( intPosLeftGuardChar > ListInfo.INDEXOF_NOT_FOUND )
                    {
                        intNChars2Append = (   intPosLeftGuardChar
                                             - intPosCurrent )
                                           + MagicNumbers.PLUS_ONE;
                        builder.Append (
                            strInputFileContent.Substring (
                                intPosCurrent ,
                                intNChars2Append ) );
                        fInsideGuardedBlock = true;
                    }   // TRUE (anticipated outcome, except perhaps for the last block), block, if ( intPosLeftGuardChar > ListInfo.INDEXOF_NOT_FOUND )
                    else
                    {   // Unless scanning for an opening guard character, this condition is invalid.
                        fLastGuardAccountedFor = true;
                        intNChars2Append = MagicNumbers.ZERO;
                    }   // FALSE (unanticipated outcome, except perhaps for the last block), block, if ( intPosLeftGuardChar > ListInfo.INDEXOF_NOT_FOUND )
                }   // FALSE (intPosCurrent is OUTSIDE a guarded block.) block, if ( fInsideGuardedBlock )

                //  ------------------------------------------------------------
                //  The less hand copying I do the sooner I can put this to bed.
                //  ------------------------------------------------------------

                intPosCurrent += intNChars2Append;

                if ( intPosCurrent > intNInputChars )
                {   // Bounds check the current position against the length of the input string.
                    throw new Exception (
                        string.Format (
                            Properties.Resources.ERRMSG_BAD_POSITION_INDEX ,
                            intPosCurrent ,                                     // Format Item 0: Position Index     = {0}
                            intNInputChars ,                                    // Format Item 1: Characters in File = {1}
                            Environment.NewLine ) );                            // Format Item 2: Line break
                }   // if ( intPosCurrent > intNInputChars )

                if ( fLastGuardAccountedFor )
                {   // If we've found the last guard character, check for remaining unguarded characters.
                    if ( intPosCurrent < intNInputChars )
                    {   // Append the trailing unguarded characters to the end of the string.
                        intNChars2Append = intNInputChars - intPosCurrent;
                        builder.Append (
                            strInputFileContent.Substring (
                                intPosCurrent ,
                                intNChars2Append ) );
                    }   // if ( intPosCurrent < intNInputChars )

                    fMoreChars2Process = false;
                }   // if ( fLastGuardAccountedFor )
            }   // while ( fMoreChars2Process )

            //  ----------------------------------------------------------------
            //  CSV files often contains quirky line breaks, such as a legacy
            //  Macintosh line break (a single line feed) followed by a standard
            //  Windows lime break (a carriage returne followed by a line feed).
            //  
            //  Method FindAndFixLimeBreakQuirks removes them, returning a count
            //  of such quirks removed. Unless the count is zero, it is reported
            //  on the 
            //  ----------------------------------------------------------------

            int intNQuirksFound = FindAndFixLimeBreakQuirks ( builder );

            if ( intNQuirksFound > MagicNumbers.ZERO )
            {
                Console.WriteLine (
                    Properties.Resources.MSG_QUIRKS_REPORT ,
                    intNQuirksFound.ToString (                                  // Format Control String
                        NumericFormats.NUMBER_PER_REG_SETTINGS_0D ) ,           // Format Item 0: {0} quirky line endings were found
                    Environment.NewLine );                                      // Format Item 1: and fixed.{1}
            }   // if ( intNQuirksFound > MagicNumbers.ZERO )

            string strOutputFileContent = builder.ToString ( );

            int intOutputCRCount = strOutputFileContent.CountCharacterOccurrences ( SpecialCharacters.CARRIAGE_RETURN );
            int intOutputLFCount = strOutputFileContent.CountCharacterOccurrences ( SpecialCharacters.LINEFEED );

            Console.WriteLine (
                Properties.Resources.MSG_LINE_BREAK_COUNTS_IN_AND_OUT ,         // Format Control String
                NumberFormatters.Integer ( intInputCRCount ) ,                  // Format Item 0: Input File:  CR Count = {0}
                NumberFormatters.Integer ( intInputLFCount ) ,                  // Format Item 1: LF Count = {1}
                NumberFormatters.Integer ( intOutputCRCount ) ,                 // Format Item 2: Output File: CR Count = {2}
                NumberFormatters.Integer ( intOutputLFCount ) ,                 // Format Item 3: LF Count = {3}
                Environment.NewLine );                                          // Format Item 4: Line break between each of the above
            File.WriteAllText (
                outFileInfo.FullName ,
                strOutputFileContent );
            System.Threading.Thread.Sleep ( MagicNumbers.MILLISECONDS_PER_SECOND );
            outFileInfo.Refresh ( );
            Console.WriteLine (
                outFileInfo.ShowFileDetails (
                    FileInfoExtensionMethods.FileDetailsToShow.Everything ,
                    Properties.Resources.MSG_OUTPUT_FILE_DETAILS ) );
        }   // private static void ProcessInputFile


        /// <summary>
        /// Scan for the next guard character in a string of delimited text, and
        /// return its position (offset) relative to its beginning.
        /// </summary>
        /// <param name="pchrGuardChar">
        /// The guard character for which to scan <paramref name="pintPosCurrent"/>
        /// </param>
        /// <param name="pstrDelimitedText">
        /// String containing delimited text to scan for <paramref name="pchrGuardChar"/>
        /// </param>
        /// <param name="pintPosCurrent">
        /// Position (offset) in <paramref name="pstrDelimitedText"/> that has
        /// been processed so far
        /// </param>
        /// <returns>
        /// If it finds another occurrence of <paramref name="pchrGuardChar"/>
        /// past offset <paramref name="pintPosCurrent"/> in input string
        /// <paramref name="pstrDelimitedText"/>, the offset where it was found
        /// is returned. Otherwise, the return value is
        /// ListInfo.INDEXOF_NOT_FOUND.
        /// </returns>
        /// <remarks>
        /// The algorithm must differentiate between the first and subsequent
        /// scans. While the first scan must always begin with the first
        /// character of the string, subsequent scans must look to the next
        /// character because the current position is where the last match was
        /// found. Static method ArrayInfo.OrdinalFromIndex does so by returning
        /// one greater than its input integer.
        /// </remarks>
        private static int ScanForNextGuardCharacter (
            char pchrGuardChar ,
            string pstrDelimitedText ,
            int pintPosCurrent )
        {
            if ( string.IsNullOrEmpty ( pstrDelimitedText ) )
            {   // Degenerate case 1 of 2 arises when pstrDelimitedText is null or the empty string.
                return ListInfo.INDEXOF_NOT_FOUND;
            }   // if ( string.IsNullOrEmpty ( pstrDelimitedText ) )

            if ( pintPosCurrent > pstrDelimitedText.Length )
            {   // Degenerate case 2 of 2 is that the start position is past the end of the string.
                return ListInfo.INDEXOF_NOT_FOUND;
            }   // if ( pintPosCurrent > pstrDelimitedText.Length )

            return pstrDelimitedText.IndexOf (
                pchrGuardChar ,
                pintPosCurrent );
        }   // private static int ScanForNextGuardCharacter


        /// <summary>
        /// Map the CMD_ARG_NEWLINE_REPL_TOKEN string values to a corresponding
        /// character.
        /// </summary>
        /// <param name="pcmdLneArgs">
        /// Passing the CmdLneArgsBasic object into the method hides the name of
        /// the corresponding command line argument, which is read into a local
        /// argument string from an embedded string resource.
        /// </param>
        /// <returns>
        /// The return value is one of two supported characters, a regular space
        /// and a nonbreaking space, both of which are defined as consnstants in
        /// core library WizardWrx.Common.dll, taken or converted to strings.
        /// </returns>
        private static string SetEmbeddedNewlineReplacementValue ( CmdLneArgsBasic pcmdLneArgs )
        {
            string strCmdArgValue = pcmdLneArgs.GetArgByName ( Properties.Resources.CMD_ARG_NEWLINE_REPL_TOKEN );

            if ( strCmdArgValue.Equals ( Properties.Resources.NEWLINE_REPL_TOKEN_SPACE ) )

                return SpecialStrings.TAB_CHAR;
            else if ( strCmdArgValue.Equals ( Properties.Resources.NEWLINE_REPL_TOKEN_NBSP ) )
                return SpecialCharacters.NONBREAKING_SPACE_CHAR.ToString ( );
            else
                return SpecialCharacters.NONBREAKING_SPACE_CHAR.ToString ( );
        }   // private static char SetEmbeddedNewlineReplacementValue
    }   // class Program
}   // namespace FixCSV