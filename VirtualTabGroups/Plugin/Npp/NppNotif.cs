namespace VirtualTabGroups.Plugin.Npp
{
    /// <summary>
    /// Notepad++ notification codes. Values must match
    /// <c>PowerEditor/src/MISC/PluginsManager/Notepad_plus_msgs.h</c> exactly: a wrong
    /// offset does not fail to compile, it silently routes the event into the wrong
    /// branch, which is how a rename went unnoticed (FILERENAMED was declared as
    /// NPPN_FIRST + 31, the native-language-changed code).
    /// </summary>
    public enum NppNotif : uint
    {
        NPPN_FIRST = 1000,
        NPPN_READY = NPPN_FIRST + 1,
        NPPN_TBMODIFICATION = NPPN_FIRST + 2,
        NPPN_FILEBEFORECLOSE = NPPN_FIRST + 3,
        NPPN_FILEOPENED = NPPN_FIRST + 4,
        NPPN_FILECLOSED = NPPN_FIRST + 5,
        NPPN_FILEBEFOREOPEN = NPPN_FIRST + 6,
        NPPN_FILEBEFORESAVE = NPPN_FIRST + 7,
        NPPN_FILESAVED = NPPN_FIRST + 8,
        NPPN_SHUTDOWN = NPPN_FIRST + 9,
        NPPN_BUFFERACTIVATED = NPPN_FIRST + 10,
        NPPN_LANGCHANGED = NPPN_FIRST + 11,
        NPPN_WORDSTYLESUPDATED = NPPN_FIRST + 12,
        NPPN_SHORTCUTREMAPPED = NPPN_FIRST + 13,
        /// <summary>
        /// Shutdown triggered, open files are still there. Used to suppress per-file
        /// cleanup that would otherwise strip every open document out of the tree.
        /// </summary>
        NPPN_BEFORESHUTDOWN = NPPN_FIRST + 19,
        /// <summary>The user backed out of shutdown (e.g. cancelled the save prompt).</summary>
        NPPN_CANCELSHUTDOWN = NPPN_FIRST + 20,
        /// <summary>
        /// Fires after the user renamed a file through Notepad++'s own Rename dialog.
        /// idFrom = bufferId; the buffer already points at the new path.
        /// </summary>
        NPPN_FILERENAMED = NPPN_FIRST + 23,
        NPPN_DARKMODECHANGED = NPPN_FIRST + 27,
    }
}
