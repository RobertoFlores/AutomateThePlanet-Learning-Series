﻿// <copyright file="ElementFinderService - Copy.cs" company="Automate The Planet Ltd.">
// Copyright 2018 Automate The Planet Ltd.
// Licensed under the Apache License, Version 2.0 (the "License");
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
// <author>Anton Angelov</author>
// <site>http://automatetheplanet.com/</site>
using ArtOfTest.WebAii.Core;
using HybridTestFramework.UITests.Core;
using HybridTestFramework.UITests.Core.Utilities;
using HybridTestFramework.UITests.Core.Utilities.ExceptionsAnalysis.AmbientContext;
using HybridTestFramework.UITests.Core.Utilities.ExceptionsAnalysis.ChainOfResponsibility;
using Unity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Unity;
using Unity.Resolution;

namespace HybridTestFramework.UITests.TestingFramework.Engine
{
    public class ExceptionAnalizedElementFinderService
    {
        private readonly IUnityContainer _container;
        private readonly IExceptionAnalyzer _excepionAnalyzer;

        public ExceptionAnalizedElementFinderService(IUnityContainer container, IExceptionAnalyzer excepionAnalyzer)
        {
            this._container = container;
            this._excepionAnalyzer = excepionAnalyzer;
        }

        public TElement Find<TElement>(IDriver driver, Find findContext, By by)
            where TElement : class, Core.Controls.IElement
        {
            var result = default(TElement);
            try
            {
                var testingFrameworkExpression = by.ToTestingFrameworkExpression();
                WaitForExists(driver, testingFrameworkExpression);
                var element = findContext.ByExpression(by.ToTestingFrameworkExpression());
                result = ResolveElement<TElement>(driver, element);
            }
            catch (Exception ex)
            {
                ////# region 9. Failed Tests Аnalysis- Chain of Responsibility Design Pattern
                ////this.excepionAnalyzer.MainApplicationHandler.HandleRequest(ex, driver);
                ////#endregion
                # region 10. Failed Tests Аnalysis- Ambient Context Design Pattern
                var mainApplicationHandler = UnityContainerFactory.GetContainer().Resolve<Handler>(ExceptionAnalyzerConstants.MainApplicationHandlerName);
                mainApplicationHandler.HandleRequest(ex, driver);
                #endregion
                throw;
            }

            return result;
        }

        public IEnumerable<TElement> FindAll<TElement>(IDriver driver, Find findContext, By by)
            where TElement : class, Core.Controls.IElement
        {
            var resolvedElements = new List<TElement>();
            try
            {
                var testingFrameworkExpression = by.ToTestingFrameworkExpression();
                WaitForExists(driver, testingFrameworkExpression);
                var elements = findContext.AllByExpression(testingFrameworkExpression);

                foreach (var currentElement in elements)
                {
                    var result = ResolveElement<TElement>(driver, currentElement);
                    resolvedElements.Add(result);
                }
            }
            catch (Exception ex)
            {
                ////# region 9. Failed Tests Аnalysis- Chain of Responsibility Design Pattern
                ////this.excepionAnalyzer.MainApplicationHandler.HandleRequest(ex, driver);
                ////#endregion
                # region 10. Failed Tests Аnalysis- Ambient Context Design Pattern
                var mainApplicationHandler = UnityContainerFactory.GetContainer().Resolve<Handler>(ExceptionAnalyzerConstants.MainApplicationHandlerName);
                mainApplicationHandler.HandleRequest(ex, driver);
                #endregion
                throw;
            }

            return resolvedElements;
        }

        public bool IsElementPresent(Find findContext, By by)
        {
            try
            {
                var controlFindExpression = by.ToTestingFrameworkExpression();
                Manager.Current.ActiveBrowser.RefreshDomTree();
                var hfe = new HtmlFindExpression(controlFindExpression);
                Manager.Current.ActiveBrowser.WaitForElement(hfe, 5000, false);
            }
            catch (TimeoutException)
            {
                return false;
            }

            return true;
        }

        private void WaitForExists(IDriver driver, string findExpression)
        {
            try
            {
                driver.WaitUntilReady();
                var hfe = new HtmlFindExpression(findExpression);
                Manager.Current.ActiveBrowser.WaitForElement(hfe, 5000, false);
            }
            catch (Exception)
            {
                ThrowTimeoutExceptionIfElementIsNull(driver, findExpression);
            }
        }

        private TElement ResolveElement<TElement>(
            IDriver driver,
            ArtOfTest.WebAii.ObjectModel.Element element)
            where TElement : class, Core.Controls.IElement
        {
            var result = _container.Resolve<TElement>(
                new ResolverOverride[]
                {
                    new ParameterOverride("driver", driver),
                    new ParameterOverride("element", element),
                    new ParameterOverride("container", _container)
                });
            return result;
        }

        private void ThrowTimeoutExceptionIfElementIsNull(IDriver driver, params string[] customExpression)
        {
            var stackTrace = new StackTrace();
            var stackFrames = stackTrace.GetFrames();
            var callingFrame = stackFrames[3];
            var method = callingFrame.GetMethod();
            var currentUrl = driver.Url;
            throw new ElementTimeoutException(
                string.Format(
                    "TIMED OUT- for element with Find Expression:\n {0}\n Element Name: {1}.{2}\n URL: {3}\nElement Timeout: {4}",
                    string.Join(",", customExpression.Select(p => p.ToString()).ToArray()),
                    method.ReflectedType.FullName, method.Name, currentUrl, Manager.Current.Settings.ElementWaitTimeout));
        }
    }
}