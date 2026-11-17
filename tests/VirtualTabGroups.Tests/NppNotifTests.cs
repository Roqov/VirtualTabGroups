using VirtualTabGroups.Plugin.Npp;
using Xunit;

namespace VirtualTabGroups.Tests
{
    /// <summary>
    /// Pins the notification codes to Notepad++'s Notepad_plus_msgs.h. A shifted
    /// constant compiles fine and silently routes an event to the wrong branch:
    /// FILERENAMED used to be NPPN_FIRST + 31, so renames from Notepad++ never reached
    /// the panel at all. These values are the contract, not an implementation detail.
    /// </summary>
    public class NppNotifTests
    {
        [Theory]
        [InlineData(NppNotif.NPPN_READY, 1001)]
        [InlineData(NppNotif.NPPN_TBMODIFICATION, 1002)]
        [InlineData(NppNotif.NPPN_FILEBEFORECLOSE, 1003)]
        [InlineData(NppNotif.NPPN_FILEOPENED, 1004)]
        [InlineData(NppNotif.NPPN_FILECLOSED, 1005)]
        [InlineData(NppNotif.NPPN_FILEBEFOREOPEN, 1006)]
        [InlineData(NppNotif.NPPN_FILEBEFORESAVE, 1007)]
        [InlineData(NppNotif.NPPN_FILESAVED, 1008)]
        [InlineData(NppNotif.NPPN_SHUTDOWN, 1009)]
        [InlineData(NppNotif.NPPN_BUFFERACTIVATED, 1010)]
        [InlineData(NppNotif.NPPN_LANGCHANGED, 1011)]
        [InlineData(NppNotif.NPPN_WORDSTYLESUPDATED, 1012)]
        [InlineData(NppNotif.NPPN_SHORTCUTREMAPPED, 1013)]
        [InlineData(NppNotif.NPPN_BEFORESHUTDOWN, 1019)]
        [InlineData(NppNotif.NPPN_CANCELSHUTDOWN, 1020)]
        [InlineData(NppNotif.NPPN_FILERENAMED, 1023)]
        [InlineData(NppNotif.NPPN_DARKMODECHANGED, 1027)]
        public void NotificationCode_MatchesNotepadPlusPlusHeader(NppNotif code, int expected)
        {
            Assert.Equal(expected, (int)code);
        }

        [Fact]
        public void FileBeforeCloseAndFileClosed_AreNotSwapped()
        {
            Assert.Equal(1003, (int)NppNotif.NPPN_FILEBEFORECLOSE);
            Assert.Equal(1005, (int)NppNotif.NPPN_FILECLOSED);
        }
    }
}
