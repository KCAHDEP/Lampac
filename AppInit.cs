﻿using Lampac.Models.SISI;
using Lampac.Models.JAC;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.IO;
using Lampac.Models.LITE.Filmix;
using Lampac.Models.LITE;
using Lampac.Models.LITE.HDVB;
using System.Collections.Generic;
using Lampac.Models.DLNA;
using Lampac.Models.AppConf;

namespace Lampac
{
    public class AppInit
    {
        #region AppInit
        public static AppInit conf = new AppInit();

        public static string Host(HttpContext httpContext) => $"http://{httpContext.Request.Host.Value}";

        static AppInit()
        {
            if (File.Exists("init.conf"))
                conf = JsonConvert.DeserializeObject<AppInit>(File.ReadAllText("init.conf"));
        }
        #endregion


        public int listenport = 9118;

        public string ffprobe = "linux";

        public bool disableserverproxy = false;

        public bool proxytoproxyimg = false;

        public bool multiaccess = false;


        public FileCacheConf fileCacheInactiveDay = new FileCacheConf() { html = 10, img = 1, torrent = 90 };

        public DLNASettings dlna = new DLNASettings() { enable = true, autoupdatetrackers = true };

        public WebConf LampaWeb = new WebConf() { autoupdate = true, index = "lampa-main/index.html" };

        public SisiConf sisi = new SisiConf() { heightPicture = 200 };

        public OnlineConf online = new OnlineConf() { findkp = "alloha", checkOnlineSearch = true };

        public JacConf jac = new JacConf();


        public TrackerSettings Rutor = new TrackerSettings("http://rutor.info", priority: "torrent");

        public TrackerSettings Megapeer = new TrackerSettings("http://megapeer.vip");

        public TrackerSettings TorrentBy = new TrackerSettings("http://torrent.by", priority: "torrent");

        public TrackerSettings Kinozal = new TrackerSettings("http://kinozal.tv");

        public TrackerSettings NNMClub = new TrackerSettings("https://nnmclub.to");

        public TrackerSettings Bitru = new TrackerSettings("https://bitru.org");

        public TrackerSettings Toloka = new TrackerSettings("https://toloka.to", enable: false);

        public TrackerSettings Rutracker = new TrackerSettings("https://rutracker.net", enable: false, priority: "torrent");

        public TrackerSettings Underverse = new TrackerSettings("https://underver.se", enable: false);

        public TrackerSettings Selezen = new TrackerSettings("https://selezen.org", enable: false, priority: "torrent");

        public TrackerSettings Anilibria = new TrackerSettings("https://www.anilibria.tv");

        public TrackerSettings Animelayer = new TrackerSettings("http://animelayer.ru", enable: false);



        public SisiSettings BongaCams = new SisiSettings("https://rt.bongacams.com");

        public SisiSettings Chaturbate = new SisiSettings("https://chaturbate.com");

        public SisiSettings Ebalovo = new SisiSettings("https://www.ebalovo.pro");

        public SisiSettings Eporner = new SisiSettings("https://www.eporner.com");

        public SisiSettings HQporner = new SisiSettings("https://hqporner.com");

        public SisiSettings Porntrex = new SisiSettings("https://www.porntrex.com");

        public SisiSettings Spankbang = new SisiSettings("https://ru.spankbang.com");

        public SisiSettings Xhamster = new SisiSettings("https://ru.xhamster.com");

        public SisiSettings Xnxx = new SisiSettings("https://www.xnxx.com");

        public SisiSettings Xvideos = new SisiSettings("https://www.xvideos.com");

        public SisiSettings PornHub = new SisiSettings("https://rt.pornhub.com");



        public OnlinesSettings Kinobase = new OnlinesSettings("https://kinobase.org");

        public OnlinesSettings Rezka = new OnlinesSettings("https://voidboost.net");

        public OnlinesSettings Collaps = new OnlinesSettings("https://api.delivembd.ws");

        public OnlinesSettings Ashdi = new OnlinesSettings("https://base.ashdi.vip");

        public OnlinesSettings Eneyida = new OnlinesSettings("https://eneyida.tv");

        public OnlinesSettings Kinokrad = new OnlinesSettings("https://kinokrad.cc");

        public OnlinesSettings Kinotochka = new OnlinesSettings("https://kinotochka.co");

        public OnlinesSettings Redheadsound = new OnlinesSettings("https://redheadsound.ru");

        public OnlinesSettings Kinoprofi = new OnlinesSettings("https://kinoprofi.vip", apihost: "https://api.kinoprofi.vip");

        public OnlinesSettings Lostfilmhd = new OnlinesSettings("http://www.lostfilmhd.ru");

        public FilmixSettings Filmix = new FilmixSettings("http://filmixapp.cyou");

        public OnlinesSettings Zetflix = new OnlinesSettings("https://zetfix.online");

        public OnlinesSettings VideoDB = new OnlinesSettings(null);

        public OnlinesSettings CDNmovies = new OnlinesSettings("https://cdnmovies.nl");


        public OnlinesSettings VCDN = new OnlinesSettings(null, apihost: "https://89442664434375553.svetacdn.in/0HlZgU1l1mw5");

        public OnlinesSettings VideoAPI = new OnlinesSettings("http://5100.svetacdn.in", token: "qR0taraBKvEZULgjoIRj69AJ7O6Pgl9O");

        public IframeVideoSettings IframeVideo = new IframeVideoSettings("https://iframe.video", "https://videoframe.space");

        public HDVBSettings HDVB = new HDVBSettings("https://apivb.info", "5e2fe4c70bafd9a7414c4f170ee1b192");

        public OnlinesSettings Seasonvar = new OnlinesSettings(null, apihost: "http://api.seasonvar.ru");

        public KinoPubSettings KinoPub = new KinoPubSettings("https://api.service-kp.com");

        public BazonSettings Bazon = new BazonSettings("https://bazon.cc", "", true);

        public AllohaSettings Alloha = new AllohaSettings("https://api.alloha.tv", "https://torso.as.alloeclub.com", "", "", true);

        public KodikSettings Kodik = new KodikSettings("https://kodikapi.com", "http://kodik.biz", "b7cc4293ed475c4ad1fd599d114f4435", "", true);


        public OnlinesSettings AnilibriaOnline = new OnlinesSettings("https://www.anilibria.tv", apihost: "https://api.anilibria.tv");

        public OnlinesSettings AniMedia = new OnlinesSettings("https://online.animedia.tv");

        public OnlinesSettings AnimeGo = new OnlinesSettings("https://animego.org");

        public OnlinesSettings Animevost = new OnlinesSettings("https://animevost.org");

        public OnlinesSettings Animebesst = new OnlinesSettings("https://anime1.animebesst.org");


        public ProxySettings proxy = new ProxySettings();

        public List<ProxySettings> globalproxy = new List<ProxySettings>() 
        {
            new ProxySettings() 
            {
                pattern = "\\.onion",
                list = new List<string>() { "socks5://127.0.0.1:9050" }
            }
        };
    }
}
