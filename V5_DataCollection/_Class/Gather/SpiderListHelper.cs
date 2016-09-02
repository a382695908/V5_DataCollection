﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using V5_DataCollection._Class.Common;
using V5_Model;
using V5_Utility.Utility;
using V5_WinLibs.Core;

namespace V5_DataCollection._Class.Gather {

    public class SpiderListHelper {

        /// <summary>
        /// 任务模型
        /// </summary>
        public ModelTask Model { get; set; } = new ModelTask();

        public delegate void OutMessage(string msg);
        public event OutMessage OutMessageHandler;

        public delegate void OutTreeNode(string url, string title, int nodeIndex);
        public event OutTreeNode OutTreeNodeHandler;

        #region 测试地址
        /// <summary>
        /// 分析列表连接
        /// </summary>
        /// <param name="Url"></param>
        public void AnalyzeSingleList(string Url) {
            var listUrl = cGatherFunction.Instance.SplitWebUrl(Url);
            for (int i = 0; i < listUrl.Count; i++) {
                ResolveList(listUrl[i], i);
            }
        }
        /// <summary>
        /// 解析列表连接
        /// </summary>
        /// <param name="testUrl"></param>
        /// <param name="num"></param>
        public void ResolveList(string testUrl, int num) {

            string pageContent = CommonHelper.getPageContent(testUrl, Model.PageEncode);

            if (Model.LinkUrlCutAreaStart?.Trim() != "" && Model.LinkUrlCutAreaEnd?.Trim() != "") {
                pageContent = HtmlHelper.Instance.ParseCollectionStrings(pageContent);
                pageContent = CollectionHelper.Instance.GetBody(pageContent,
                    HtmlHelper.Instance.ParseCollectionStrings(Model.LinkUrlCutAreaStart),
                    HtmlHelper.Instance.ParseCollectionStrings(Model.LinkUrlCutAreaEnd),
                    false,
                    false);

                if (pageContent == "本次请求并未返回任何数据") {
                    OutMessageHandler?.Invoke("采集失败!");
                    return;
                }

                pageContent = HtmlHelper.Instance.UnParseCollectionStrings(pageContent);
            }

            string regexHref = cRegexHelper.RegexATag;
            int i = 0;
            if (Model.IsHandGetUrl == 1) {
                regexHref = Model.HandCollectionUrlRegex;
                regexHref = regexHref.Replace("[", "\\[");
                regexHref = regexHref.Replace("\\[参数]", "[参数]");
                regexHref = regexHref.Replace("(*)", ".+?");

                while (regexHref.IndexOf("[参数]") >= 0) {
                    i++;
                    int tmp = regexHref.IndexOf("[参数]"); //获取[参数]第一次出现的索引值
                    regexHref = regexHref.Remove(tmp, "[参数]".Length); //在该索引处删除[参数]
                    regexHref = regexHref.Insert(tmp, "(?<参数" + i + ">.+?)"); // 在该索引出插入112
                }
            }

            Match mch = null;
            Regex reg = new Regex(regexHref, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            string url = string.Empty;
            string title = string.Empty;
            string strUrl = string.Empty;
            MatchCollection matches = reg.Matches(pageContent);
            for (mch = reg.Match(pageContent); mch.Success; mch = mch.NextMatch()) {
                url = CollectionHelper.Instance.FormatUrl(testUrl, mch.Groups[1].Value);
                title = mch.Groups[2].Value;

                if (Model.LinkUrlMustIncludeStr.Trim() != "") {
                    if (url.IndexOf(Model.LinkUrlMustIncludeStr) == -1) {
                        continue;
                    }
                }
                if (Model.LinkUrlNoMustIncludeStr.Trim() != "") {
                    bool isFlag = true;
                    foreach (string str in Model.LinkUrlNoMustIncludeStr.Split(new string[] { "||" }, StringSplitOptions.RemoveEmptyEntries)) {
                        if (url.IndexOf(str) > -1) {
                            isFlag = false;
                            break;
                        }
                    }
                    if (isFlag)
                        OutTreeNodeHandler?.Invoke(url, title, num);
                }
                else {
                    OutTreeNodeHandler?.Invoke(url, title, num);
                }
            }
        }

        /// <summary>
        /// 分析列表
        /// </summary>
        public void AnalyzeAllList() {
            OutMessageHandler?.Invoke("正在分析采集列表个数!");

            foreach (string linkUrl in Model.CollectionContent.Split(new string[] { "$$$$" }, StringSplitOptions.RemoveEmptyEntries)) {
                try {
                    AnalyzeSingleList(linkUrl);
                }
                catch (Exception ex1) {
                    Log4Helper.Write(LogLevel.Error, ex1.StackTrace, ex1);
                    continue;
                }
            }
        }
        #endregion



    }


}