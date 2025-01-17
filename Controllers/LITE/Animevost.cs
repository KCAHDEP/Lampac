﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Lampac.Engine;
using Lampac.Engine.CORE;
using System.Web;
using Microsoft.Extensions.Caching.Memory;
using System.Linq;
using System.Text.RegularExpressions;

namespace Lampac.Controllers.LITE
{
    public class Animevost : BaseController
    {
        [HttpGet]
        [Route("lite/animevost")]
        async public Task<ActionResult> Index(string title, int year, string uri, int s)
        {
            if (!AppInit.conf.Animevost.enable || string.IsNullOrWhiteSpace(title))
                return Content(string.Empty);

            bool firstjson = true;
            string html = "<div class=\"videos__line\">";

            if (string.IsNullOrWhiteSpace(uri))
            {
                #region Поиск
                string memkey = $"animevost:search:{title}";
                if (!memoryCache.TryGetValue(memkey, out List<(string title, string uri, string s)> catalog))
                {
                    string search = await HttpClient.Post($"{AppInit.conf.Animevost.host}/index.php?do=search", $"do=search&subaction=search&search_start=0&full_search=1&result_from=1&story={HttpUtility.UrlEncode(title)}&all_word_seach=1&titleonly=3&searchuser=&replyless=0&replylimit=0&searchdate=0&beforeafter=after&sortby=date&resorder=desc&showposts=0&catlist%5B%5D=0", timeoutSeconds: 8, useproxy: AppInit.conf.Animevost.useproxy);
                    if (search == null)
                        return Content(string.Empty);

                    catalog = new List<(string title, string uri, string s)>();

                    foreach (string row in search.Split("class=\"shortstory\"").Skip(1))
                    {
                        var g = Regex.Match(row, "<a href=\"(https?://[^\"]+\\.html)\">([^<]+)</a>").Groups;
                        string animeyear = Regex.Match(row, "<strong>Год выхода: ?</strong>([0-9]{4})</p>").Groups[1].Value;

                        if (!string.IsNullOrWhiteSpace(g[1].Value) && !string.IsNullOrWhiteSpace(g[2].Value))
                        {
                            string season = "0";
                            if (animeyear == year.ToString() && g[2].Value.ToLower().StartsWith(title.ToLower()))
                                season = "1";

                            catalog.Add((g[2].Value, g[1].Value, season));
                        }
                    }

                    if (catalog.Count == 0)
                        return Content(string.Empty);

                    memoryCache.Set(memkey, catalog, DateTime.Now.AddMinutes(AppInit.conf.multiaccess ? 40 : 10));
                }

                if (catalog.Count == 1)
                    return LocalRedirect($"/lite/animevost?title={HttpUtility.UrlEncode(title)}&uri={HttpUtility.UrlEncode(catalog[0].uri)}&s={catalog[0].s}");

                foreach (var res in catalog)
                {
                    string link = $"{AppInit.Host(HttpContext)}/lite/animevost?title={HttpUtility.UrlEncode(title)}&uri={HttpUtility.UrlEncode(res.uri)}&s={res.s}";

                    html += "<div class=\"videos__item videos__season selector " + (firstjson ? "focused" : "") + "\" data-json='{\"method\":\"link\",\"url\":\"" + link + "\",\"similar\":true}'><div class=\"videos__season-layers\"></div><div class=\"videos__item-imgbox videos__season-imgbox\"><div class=\"videos__item-title videos__season-title\">" + res.title + "</div></div></div>";
                    firstjson = false;
                }
                #endregion
            }
            else 
            {
                #region Серии
                string memKey = $"animevost:playlist:{uri}";
                if (!memoryCache.TryGetValue(memKey, out List<(string episode, string id)> links))
                {
                    string news = await HttpClient.Get(uri, timeoutSeconds: 10, useproxy: AppInit.conf.Animevost.useproxy);
                    if (string.IsNullOrWhiteSpace(news))
                        return Content(string.Empty);

                    string data = Regex.Match(news, "var data = ([^\n\r]+)").Groups[1].Value;
                    if (string.IsNullOrWhiteSpace(data))
                        return Content(string.Empty);

                    links = new List<(string episode, string id)>();
                    var match = Regex.Match(data, "\"([^\"]+)\":\"([0-9]+)\",");
                    while (match.Success)
                    {
                        if (!string.IsNullOrWhiteSpace(match.Groups[1].Value) && !string.IsNullOrWhiteSpace(match.Groups[2].Value))
                            links.Add((match.Groups[1].Value, match.Groups[2].Value));

                        match = match.NextMatch();
                    }

                    if (links.Count == 0)
                        return Content(string.Empty);

                    memoryCache.Set(memKey, links, DateTime.Now.AddMinutes(AppInit.conf.multiaccess ? 30 : 10));
                }

                foreach (var l in links)
                {
                    string link = $"{AppInit.Host(HttpContext)}/lite/animevost/video?id={l.id}";

                    html += "<div class=\"videos__item videos__movie selector " + (firstjson ? "focused" : "") + "\" media=\"\" s=\"" + s + "\" e=\"" + Regex.Match(l.episode, "^([0-9]+)").Groups[1].Value + "\" data-json='{\"method\":\"play\",\"url\":\"" + link + "\",\"title\":\"" + $"{title} ({l.episode})" + "\"}'><div class=\"videos__item-imgbox videos__movie-imgbox\"></div><div class=\"videos__item-title\">" + l.episode + "</div></div>";
                    firstjson = true;
                }
                #endregion
            }

            return Content(html + "</div>", "text/html; charset=utf-8");
        }


        #region Video
        [HttpGet]
        [Route("lite/animevost/video")]
        async public Task<ActionResult> Video(int id)
        {
            if (!AppInit.conf.Animevost.enable)
                return Content(string.Empty);

            string memKey = $"animevost:video:{id}";
            if (!memoryCache.TryGetValue(memKey, out string mp4))
            {
                string iframe = await HttpClient.Get($"{AppInit.conf.Animevost.host}/frame5.php?play={id}&old=1", timeoutSeconds: 8);
                if (string.IsNullOrWhiteSpace(iframe))
                    return Content(string.Empty);

                mp4 = Regex.Match(iframe, "download=\"invoice\"[^>]+href=\"(https?://[^\"]+)\">720p").Groups[1].Value;
                if (string.IsNullOrWhiteSpace(mp4))
                    mp4 = Regex.Match(iframe, "download=\"invoice\"[^>]+href=\"(https?://[^\"]+)\">480p").Groups[1].Value;

                if (string.IsNullOrWhiteSpace(mp4))
                    return Content(string.Empty);

                memoryCache.Set(memKey, mp4, DateTime.Now.AddMinutes(AppInit.conf.multiaccess ? 20 : 10));
            }

            return Redirect($"{AppInit.Host(HttpContext)}/proxy/{mp4}");
        }
        #endregion
    }
}
