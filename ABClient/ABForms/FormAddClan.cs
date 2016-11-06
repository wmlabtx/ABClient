﻿using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ABClient.AppControls;
using System;
using ABClient.MyHelpers;

namespace ABClient.ABForms
{
    //internal delegate void FormAddClanStopDelegate();

    //internal delegate void AddCharacterFromClanDelegate(string htmlCharacter);
    internal delegate void UpdateStatusFormAddClanDelegate(string status);

    internal partial class FormAddClan : Form
    {
        private readonly StringBuilder _sb = new StringBuilder();

        internal FormAddClan(string nick)
        {
            InitializeComponent();
            wb.BeforeNavigate += WbBeforeNavigate;
            _sb.Append(
                "<html>" +
                "<head>" +
                @"<meta content=""text/html; charset=windows-1251"" http-equiv=Content-type>" +
                "<META Http-Equiv=Cache-Control Content=no-cache>" +
                "<body bgcolor=#FCFAF3>");
            UpdateReport();
            ProcessNick(nick);
        }

        static void WbBeforeNavigate(object sender, WebBrowserExtendedNavigatingEventArgs e)
        {
            var url = e.Address;
            if (!url.StartsWith("http://")) 
                return;
            
            if (url.IndexOf("pinfo.cgi?", StringComparison.CurrentCultureIgnoreCase) != -1)
            {
                    
                if (AppVars.MainForm != null)
                {                        
                    AppVars.MainForm.BeforeNewWindow(url);
                }
            }
            else
            {
                var regnum = url.Substring("http://".Length);
                if (regnum.EndsWith("/"))
                {
                    regnum = regnum.Substring(0, regnum.Length - 1);
                    if (AppVars.MainForm != null)
                    {
                        e.Cancel = true;
                        AppVars.MainForm.MoveToDialog(regnum);
                    }                    
                }
            }

            e.Cancel = true;
        }

        internal void SetNick(string nick)
        {
            ProcessNick(nick);
        }

        internal void UpdateStatus(string message)
        {
            status.Text = message;
        }

        internal void AddCharacter(string htmlCharacter)
        {
            _sb.Append(htmlCharacter);
            UpdateReport();
        }

        private void UpdateReport()
        {
            wb.DocumentText = _sb + "</body></html>";
        }

        private static void ProcessNick(string nick)
        {
            ThreadPool.QueueUserWorkItem(AddClanAsync, nick);
        }

        private void FormAddClanFormClosed(object sender, FormClosedEventArgs e)
        {
            AppVars.VipFormAddClan = null;
        }

        /* in44.nl
        private static void AddClanAsync(object stateInfo)
        {
            var nick = stateInfo as string;
            if (nick == null)
            {
                return;
            }

            var html = NeverInfo.GetPInfo(nick);
            if (string.IsNullOrEmpty(html))
                return;

            var params0 = HelperStrings.SubString(html, "var params = [[", "],");
            if (string.IsNullOrEmpty(params0))
            {
                return;
            }

            var spar0 = HelperStrings.ParseArguments(params0);
            if (spar0.Length < 9)
            {
                return;
            }

            var firstNick = spar0[0].Trim();
            var firstSign = spar0[2];
            var firstClan = spar0[8];

            var listClanNicks = new List<string> {firstNick};
            if (!string.IsNullOrEmpty(firstSign))
            {
                byte[] buffer;
                using (var wc = new WebClient {Proxy = AppVars.LocalProxy})
                {
                    try
                    {
                        var suff = firstSign;
                        var posp = suff.IndexOf('.');
                        if (posp != -1)
                            suff = suff.Substring(0, posp);

                        buffer = wc.DownloadData(new Uri("http://in44.nl/clanstruct/c_" + suff + "/"));
                    }
                    catch (WebException)
                    {
                        return;
                    }
                }

                html = AppVars.Codepage.GetString(buffer);
                var p1 = 0;
                while (p1 != -1)
                {
                    p1 = html.IndexOf("pinfo.cgi?", p1, StringComparison.OrdinalIgnoreCase);
                    if (p1 == -1)
                        break;

                    p1 += "pinfo.cgi?".Length;
                    var p2 = html.IndexOf(@"""", p1, StringComparison.OrdinalIgnoreCase);
                    if (p2 == -1)
                        continue;

                    var statnick = html.Substring(p1, p2 - p1);
                    listClanNicks.Add(statnick);
                }
            }

            foreach (var vipNick in listClanNicks)
            {
                try
                {
                    if (AppVars.VipFormAddClan != null)
                    {
                        var message = string.Format("Анализируем [{0}]...", vipNick);
                        AppVars.VipFormAddClan.BeginInvoke(
                            new UpdateStatusFormAddClanDelegate(AppVars.VipFormAddClan.UpdateStatus),
                            new object[] { message });
                    }
                    else
                    {
                        return;
                    }
                }
                catch (InvalidOperationException)
                {
                }

                html = NeverInfo.GetPInfo(vipNick);
                if (string.IsNullOrEmpty(html))
                    continue;

                params0 = HelperStrings.SubString(html, "var params = [[", "],");
                if (string.IsNullOrEmpty(params0))
                {
                    continue;
                }

                spar0 = HelperStrings.ParseArguments(params0);
                if (spar0.Length < 9)
                {
                    continue;
                }

                var infNick = spar0[0].Trim();
                var infAlign = spar0[1];
                var infSign = spar0[2];
                var infClan = spar0[8];

                if (!infClan.Equals(firstClan))
                {
                    continue;
                }

                var infLevel = spar0[3];
                var infLocation = spar0[5];
                if (infLocation.IndexOf('[') != -1)
                {
                    infLocation = HelperStrings.SubString(infLocation, "[", "]");
                }

                var infIsOnline = !string.IsNullOrEmpty(infLocation);

                var isLightWound = false;
                var isMediumWound = false;
                var isHeavyWound = false;
                var isUltimateWound = false;

                var effects = HelperStrings.SubString(html, "var effects = [", "];");
                var sbeff = new StringBuilder();
                if (!string.IsNullOrEmpty(effects))
                {
                    var seffects = effects.Split(new[] { "],[" }, StringSplitOptions.RemoveEmptyEntries);
                    for (var k = 0; k < seffects.Length; k++)
                    {
                        var effk = seffects[k].Trim(new[] { '[', ']' });
                        var seffk = effk.Split(',');
                        if (seffk.Length <= 1)
                        {
                            continue;
                        }

                        var effcode = seffk[0];
                        var effname = seffk[1].Replace("<b>", String.Empty).Replace("</b>", String.Empty);
                        sbeff.AppendFormat(
                            @"&nbsp;<img src=http://image.neverlands.ru/pinfo/eff_{0}.gif width=15 height=15 align=absmiddle alt=""{1}"">",
                            effcode,
                            effname);
                        switch (effcode)
                        {
                            case "1":
                                isUltimateWound = true;
                                break;

                            case "2":
                                isHeavyWound = true;
                                break;

                            case "3":
                                isMediumWound = true;
                                break;

                            case "4":
                                isLightWound = true;
                                break;
                        }
                    }
                }

                var travma = string.Empty;
                if (isUltimateWound)
                {
                    travma = "боевая";
                }
                else
                {
                    if (isHeavyWound)
                    {
                        travma = "тяжелая";
                    }
                    else
                    {
                        if (isMediumWound)
                        {
                            travma = "средняя";
                        }
                        else
                        {
                            if (isLightWound)
                            {
                                travma = "легкая";
                            }
                        }
                    }
                }

                var colorNick = infNick;
                if (!infIsOnline)
                {
                    colorNick = @"<font color=""#cccccc"">" + colorNick + "</font>";
                }
                else
                {
                    if (!string.IsNullOrEmpty(travma))
                    {
                        switch (travma)
                        {
                            case "боевая":
                                colorNick = @"<font color=""#666600"">" + colorNick + "</font>";
                                break;
                            case "тяжелая":
                                colorNick = @"<font color=""#c10000"">" + colorNick + "</font>";
                                break;
                            case "средняя":
                                colorNick = @"<font color=""#e94c69"">" + colorNick + "</font>";
                                break;
                            case "легкая":
                                colorNick = @"<font color=""#ef7f94"">" + colorNick + "</font>";
                                break;
                        }
                    }
                }

                var align = string.Empty;
                var ali1 = string.Empty;
                var ali2 = string.Empty;
                switch (infAlign)
                {
                    case "1":
                        ali1 = "darks.gif";
                        ali2 = "Дети Тьмы";
                        break;
                    case "2":
                        ali1 = "lights.gif";
                        ali2 = "Дети Света";
                        break;
                    case "3":
                        ali1 = "sumers.gif";
                        ali2 = "Дети Сумерек";
                        break;
                    case "4":
                        ali1 = "chaoss.gif";
                        ali2 = "Дети Хаоса";
                        break;
                    case "5":
                        ali1 = "light.gif";
                        ali2 = "Истинный Свет";
                        break;
                    case "6":
                        ali1 = "dark.gif";
                        ali2 = "Истинная Тьма";
                        break;
                    case "7":
                        ali1 = "sumer.gif";
                        ali2 = "Нейтральные Сумерки";
                        break;
                    case "8":
                        ali1 = "chaos.gif";
                        ali2 = "Абсолютный Хаос";
                        break;
                    case "9":
                        ali1 = "angel.gif";
                        ali2 = "Ангел";
                        break;
                }

                if (!string.IsNullOrEmpty(ali1))
                {
                    align =
                        "<img src=http://image.neverlands.ru/signs/" +
                        ali1 +
                        @" width=15 height=12 align=absmiddle border=0 alt=""" +
                        ali2 +
                        @""">&nbsp";
                }

                var ss = string.Empty;
                if (!string.IsNullOrEmpty(infClan))
                {
                    ss =
                        "<img src=http://image.neverlands.ru/signs/" +
                        infSign +
                        @" width=15 height=12 align=absmiddle alt=""" +
                        infClan +
                        @""">&nbsp;";
                }

                var result =
                    @"<span style=""font-family:Verdana,Arial,sans-serif; font-size:12px; text-decoration:none;	color:#222222;"">" +
                    align +
                    ss +
                    @"<b>" +
                    colorNick +
                    "</b></a>[" +
                    infLevel +
                    @"]<a href='http://www.neverlands.ru/pinfo.cgi?" + infNick + "'><img src=http://image.neverlands.ru/chat/info.gif width=11 height=12 border=0 align=absmiddle></a>" +
                    sbeff +
                    "<img src=http://image.neverlands.ru/1x1.gif width=8 height=1>" +
                    @"<span style=""font-size:10px;"">" + "добавлен в контакты" + "</span>" +
                    "</span><br>";

                try
                {
                    if (AppVars.VipFormAddClan != null)
                    {
                        AppVars.VipFormAddClan.BeginInvoke(
                            new UpdateStatusFormAddClanDelegate(AppVars.VipFormAddClan.AddCharacter),
                            new object[] { result });
                    }
                    else
                    {
                        return;
                    }
                }
                catch (InvalidOperationException)
                {
                }

                var nickContact = infNick;

                if (AppVars.MainForm != null)
                {
                    AppVars.MainForm.BeginInvoke(
                        new AddContactFromBulkDelegate(AppVars.MainForm.AddContactFromBulk),
                        new object[] { nickContact });
                }
                else
                {
                    return;
                }
            }

            try
            {
                if (AppVars.VipFormAddClan != null)
                {
                    AppVars.VipFormAddClan.BeginInvoke(
                        new UpdateStatusFormAddClanDelegate(AppVars.VipFormAddClan.UpdateStatus),
                        new object[] { "Добавление в контакты завешено." });
                }
                else
                {
                    return;
                }
            }
            catch (InvalidOperationException)
            {
            }

            AppVars.Profile.Save();
        }
        */

        private static void AddClanAsync(object stateInfo)
        {
            var nick = stateInfo as string;
            if (nick == null)
            {
                return;
            }

            var html = NeverInfo.GetPInfo(nick);
            if (string.IsNullOrEmpty(html))
                return;

            var params0 = HelperStrings.SubString(html, "var params = [[", "],");
            if (string.IsNullOrEmpty(params0))
            {
                return;
            }

            var spar0 = HelperStrings.ParseArguments(params0);
            if (spar0.Length < 9)
            {
                return;
            }

            var firstNick = spar0[0].Trim();
            var firstSign = spar0[2];
            var firstClan = spar0[8];

            var listClanNicks = new List<string> { firstNick };
            if (!string.IsNullOrEmpty(firstSign))
            {
                byte[] buffer;
                using (var wc = new WebClient { Proxy = AppVars.LocalProxy })
                {
                    try
                    {
                        IdleManager.AddActivity();
                        buffer = wc.DownloadData(new Uri("http://neverok.ru//clan_players.php?type=all"));
                    }
                    catch (WebException)
                    {
                        return;
                    }
                    finally
                    {
                        IdleManager.RemoveActivity();
                    }
                }

                html = AppVars.Codepage.GetString(buffer);
                if (string.IsNullOrEmpty(html))
                    return;

                var linkClan = HelperStrings.SubString(html, firstSign + @"""><a href=""clan_players.php?clantype=", @"""");
                if (string.IsNullOrEmpty(linkClan))
                    return;

                try
                {
                    if (AppVars.VipFormAddClan != null)
                    {
                        var message = string.Format($"Получаем список клана [{firstClan}]...");
                        AppVars.VipFormAddClan.BeginInvoke(
                            new UpdateStatusFormAddClanDelegate(AppVars.VipFormAddClan.UpdateStatus), message);
                    }
                    else
                    {
                        return;
465
                return;
466
            }
467
​
468
            var spar0 = HelperStrings.ParseArguments(params0);
469
            if (spar0.Length < 9)
470
            {
471
                return;
472
            }
473
​
474
            var firstNick = spar0[0].Trim();
475
            var firstSign = spar0[2];
476
            var firstClan = spar0[8];
477
​
478
            var listClanNicks = new List<string> { firstNick };
479
            if (!string.IsNullOrEmpty(firstSign))
480
            {
481
                byte[] buffer;
482
                using (var wc = new WebClient { Proxy = AppVars.LocalProxy })
483
                {
484
                    try
485
                    {
486
                        IdleManager.AddActivity();
487
                        buffer = wc.DownloadData(new Uri("http://neverok.ru//clan_players.php?type=all"));
488
                    }
489
                    catch (WebException)
490
                    {
491
                        return;
492
                    }
493
                    finally
494
                    {
495
                        IdleManager.RemoveActivity();
496
                    }
497
                }
498
​
499
                html = AppVars.Codepage.GetString(buffer);
500
                if (string.IsNullOrEmpty(html))
501
                    return;
502
​
503
                var linkClan = HelperStrings.SubString(html, firstSign + @"""><a href=""clan_players.php?clantype=", @"""");
504
                if (string.IsNullOrEmpty(linkClan))
505
                    return;
506
​
507
                try
508
                {
509
                    if (AppVars.VipFormAddClan != null)
510
                    {
511
                        var message = string.Format($"Получаем список клана [{firstClan}]...");
512
                        AppVars.VipFormAddClan.BeginInvoke(
513
                            new UpdateStatusFormAddClanDelegate(AppVars.VipFormAddClan.UpdateStatus), message);
514
                    }
515
                    else
516
                    {
517
                        return;
518
                    }
519
                }
520
                catch (InvalidOperationException)
521
                {
522
                }
523
​
524
                using (var wc = new WebClient { Proxy = AppVars.LocalProxy })
525
                {
526
                    try
527
                    {
528
                        IdleManager.AddActivity();
529
                        buffer = wc.DownloadData(new Uri($"http://neverok.ru/clan_players.php?clantype={linkClan}"));
530
                    }
531
                    catch (WebException)
532
                    {

                    }
                }
                catch (InvalidOperationException)
                {
                }

                using (var wc = new WebClient { Proxy = AppVars.LocalProxy })
                {
                    try
                    {
                        IdleManager.AddActivity();
                        buffer = wc.DownloadData(new Uri($"http://neverok.ru/clan_players.php?clantype={linkClan}"));
                    }
                    catch (WebException)
                    {
                        return;
                    }
                    finally
                    {
                        IdleManager.RemoveActivity();
                    }
                }

                html = AppVars.Codepage.GetString(buffer);
                if (string.IsNullOrEmpty(html))
                    return;

                var p1 = 0;
                while (p1 != -1)
                {
                    const string pat1 = @" target=""_blank"">";
                    p1 = html.IndexOf(pat1, p1, StringComparison.OrdinalIgnoreCase);
                    if (p1 == -1)
                        break;

                    p1 += pat1.Length;
                    var p2 = html.IndexOf(@"[", p1, StringComparison.OrdinalIgnoreCase);
                    if (p2 == -1)
                        continue;

                    var statnick = html.Substring(p1, p2 - p1);
                    if (statnick.Length < 64)
                        listClanNicks.Add(statnick);
                }
            }

            foreach (var vipNick in listClanNicks)
            {
                try
                {
                    if (AppVars.VipFormAddClan != null)
                    {
                        var message = $"Анализируем [{vipNick}]...";
                        AppVars.VipFormAddClan.BeginInvoke(
                            new UpdateStatusFormAddClanDelegate(AppVars.VipFormAddClan.UpdateStatus), message);
                    }
                    else
                    {
                        return;
                    }
                }
                catch (InvalidOperationException)
                {
                }

                html = NeverInfo.GetPInfo(vipNick);
                if (string.IsNullOrEmpty(html))
                    continue;

                params0 = HelperStrings.SubString(html, "var params = [[", "],");
                if (string.IsNullOrEmpty(params0))
                {
                    continue;
                }

                spar0 = HelperStrings.ParseArguments(params0);
                if (spar0.Length < 9)
                {
                    continue;
                }

                var infNick = spar0[0].Trim();
                var infAlign = spar0[1];
                var infSign = spar0[2];
                var infClan = spar0[8];

                if (!infClan.Equals(firstClan))
                {
                    continue;
                }

                var infLevel = spar0[3];
                var infLocation = spar0[5];
                if (infLocation.IndexOf('[') != -1)
                {
                    infLocation = HelperStrings.SubString(infLocation, "[", "]");
                }

                var infIsOnline = !string.IsNullOrEmpty(infLocation);

                var isLightWound = false;
                var isMediumWound = false;
                var isHeavyWound = false;
                var isUltimateWound = false;

                var effects = HelperStrings.SubString(html, "var effects = [", "];");
                var sbeff = new StringBuilder();
                if (!string.IsNullOrEmpty(effects))
                {
                    var seffects = effects.Split(new[] { "],[" }, StringSplitOptions.RemoveEmptyEntries);
                    for (var k = 0; k < seffects.Length; k++)
                    {
                        var effk = seffects[k].Trim(new[] { '[', ']' });
                        var seffk = effk.Split(',');
                        if (seffk.Length <= 1)
                        {
                            continue;
                        }

                        var effcode = seffk[0];
                        var effname = seffk[1].Replace("<b>", String.Empty).Replace("</b>", String.Empty);
                        sbeff.AppendFormat(
                            @"&nbsp;<img src=http://image.neverlands.ru/pinfo/eff_{0}.gif width=15 height=15 align=absmiddle title=""{1}"">",
                            effcode,
                            effname);
                        switch (effcode)
                        {
                            case "1":
                                isUltimateWound = true;
                                break;

                            case "2":
                                isHeavyWound = true;
                                break;

                            case "3":
                                isMediumWound = true;
                                break;

                            case "4":
                                isLightWound = true;
                                break;
                        }
                    }
                }

                var travma = string.Empty;
                if (isUltimateWound)
                {
                    travma = "боевая";
                }
                else
                {
                    if (isHeavyWound)
                    {
                        travma = "тяжелая";
                    }
                    else
                    {
                        if (isMediumWound)
                        {
                            travma = "средняя";
                        }
                        else
                        {
                            if (isLightWound)
                            {
                                travma = "легкая";
                            }
                        }
                    }
                }

                var colorNick = infNick;
                if (!infIsOnline)
                {
                    colorNick = @"<font color=""#cccccc"">" + colorNick + "</font>";
                }
                else
                {
                    if (!string.IsNullOrEmpty(travma))
                    {
                        switch (travma)
                        {
                            case "боевая":
                                colorNick = @"<font color=""#666600"">" + colorNick + "</font>";
                                break;
                            case "тяжелая":
                                colorNick = @"<font color=""#c10000"">" + colorNick + "</font>";
                                break;
                            case "средняя":
                                colorNick = @"<font color=""#e94c69"">" + colorNick + "</font>";
                                break;
                            case "легкая":
                                colorNick = @"<font color=""#ef7f94"">" + colorNick + "</font>";
                                break;
                        }
                    }
                }

                var align = string.Empty;
                var ali1 = string.Empty;
                var ali2 = string.Empty;
                switch (infAlign)
                {
                    case "1":
                        ali1 = "darks.gif";
                        ali2 = "Дети Тьмы";
                        break;
                    case "2":
                        ali1 = "lights.gif";
                        ali2 = "Дети Света";
                        break;
                    case "3":
                        ali1 = "sumers.gif";
                        ali2 = "Дети Сумерек";
                        break;
                    case "4":
                        ali1 = "chaoss.gif";
                        ali2 = "Дети Хаоса";
                        break;
                    case "5":
                        ali1 = "light.gif";
                        ali2 = "Истинный Свет";
                        break;
                    case "6":
                        ali1 = "dark.gif";
                        ali2 = "Истинная Тьма";
                        break;
                    case "7":
                        ali1 = "sumer.gif";
                        ali2 = "Нейтральные Сумерки";
                        break;
                    case "8":
                        ali1 = "chaos.gif";
                        ali2 = "Абсолютный Хаос";
                        break;
                    case "9":
                        ali1 = "angel.gif";
                        ali2 = "Ангел";
                        break;
                }

                if (!string.IsNullOrEmpty(ali1))
                {
                    align =
                        "<img src=http://image.neverlands.ru/signs/" +
                        ali1 +
                        @" width=15 height=12 align=absmiddle border=0 title=""" +
                        ali2 +
                        @""">&nbsp";
                }

                var ss = string.Empty;
                if (!string.IsNullOrEmpty(infClan))
                {
                    ss =
                        "<img src=http://image.neverlands.ru/signs/" +
                        infSign +
                        @" width=15 height=12 align=absmiddle title=""" +
                        infClan +
                        @""">&nbsp;";
                }

                var result =
                    @"<span style=""font-family:Verdana,Arial,sans-serif; font-size:12px; text-decoration:none;	color:#222222;"">" +
                    align +
                    ss +
                    @"<b>" +
                    colorNick +
                    "</b></a>[" +
                    infLevel +
                    @"]<a href='http://www.neverlands.ru/pinfo.cgi?" + infNick + "'><img src=http://image.neverlands.ru/chat/info.gif width=11 height=12 border=0 align=absmiddle></a>" +
                    sbeff +
                    "<img src=http://image.neverlands.ru/1x1.gif width=8 height=1>" +
                    @"<span style=""font-size:10px;"">" + "добавлен в контакты" + "</span>" +
                    "</span><br>";

                try
                {
                    if (AppVars.VipFormAddClan != null)
                    {
                        AppVars.VipFormAddClan.BeginInvoke(
                            new UpdateStatusFormAddClanDelegate(AppVars.VipFormAddClan.AddCharacter),
                            new object[] { result });
                    }
                    else
                    {
                        return;
                    }
                }
                catch (InvalidOperationException)
                {
                }

                var nickContact = infNick;

                if (AppVars.MainForm != null)
                {
                    AppVars.MainForm.BeginInvoke(
                        new AddContactFromBulkDelegate(AppVars.MainForm.AddContactFromBulk),
                        new object[] { nickContact });
                }
                else
                {
                    return;
                }
            }

            try
            {
                if (AppVars.VipFormAddClan != null)
                {
                    AppVars.VipFormAddClan.BeginInvoke(
                        new UpdateStatusFormAddClanDelegate(AppVars.VipFormAddClan.UpdateStatus),
                        new object[] { "Добавление в контакты завешено." });
                }
                else
                {
                    return;
                }
            }
            catch (InvalidOperationException)
            {
            }

            AppVars.Profile.Save();
        }
    }
}