namespace VirtualTabGroups.Plugin.Npp
{
    public enum NppMsg : int
    {
        NPPMSG = 0x400 + 1000,

        NPPM_GETFULLCURRENTPATH = NPPMSG + 18,
        NPPM_GETPLUGINSCONFIGDIR = NPPMSG + 46,
        NPPM_DOOPEN = NPPMSG + 77,
        NPPM_GETNBOPENFILES = NPPMSG + 7,
        NPPM_GETOPENFILENAMES = NPPMSG + 8,
        NPPM_GETFULLPATHFROMBUFFERID = NPPMSG + 58,
        NPPM_GETCURRENTBUFFERID = NPPMSG + 60,
        NPPM_GETMENUHANDLE = NPPMSG + 25,
        NPPM_MODELESSDIALOG = NPPMSG + 12,
        NPPM_DMMSHOW = NPPMSG + 30,
        NPPM_DMMHIDE = NPPMSG + 31,
        NPPM_DMMUPDATEDISPINFO = NPPMSG + 32,
        NPPM_DMMREGASDCKDLG = NPPMSG + 33,
        NPPM_DMMVIEWOTHERTABBAR = NPPMSG + 35,
        NPPM_SWITCHTOFILE = NPPMSG + 37,
        NPPM_MENUCOMMAND = NPPMSG + 48,

        NPPM_GETCURRENTDOCINDEX = NPPMSG + 47,
        // wParam = 0-based doc index, lParam = view (0 main, 1 sub). The header
        // defines this as NPPMSG + 59: NPPMSG already contains WM_USER + 1000, so
        // adding 1083 addressed a message Notepad++ does not handle and returned 0.
        NPPM_GETBUFFERIDFROMPOS = NPPMSG + 59,
        NPPM_ISDARKMODEENABLED = NPPMSG + 107,
        NPPM_GETDARKMODECOLORS = NPPMSG + 108,
        NPPM_DARKMODESUBCLASSANDTHEME = NPPMSG + 112,
    }
}
