﻿using Sdl.Desktop.IntegrationApi;
using Sdl.Desktop.IntegrationApi.Extensions;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace Trados.Transcreate
{
    [ApplicationInitializer]
    internal class ApplicationInstance : IApplicationInitializer
    {
        public static Form GetActiveForm()
        {
            var allForms = System.Windows.Forms.Application.OpenForms;
            var activeForm = allForms[allForms.Count - 1];
            foreach (Form form in allForms)
            {
                if (form.GetType().Name == "StudioWindowForm")
                {
                    activeForm = form;
                    break;
                }
            }

            return activeForm;
        }

        public void Execute()
        {
            SetApplicationShutdownMode();

            Common.Log.Setup();
        }

        private static void SetApplicationShutdownMode()
        {
            if (Application.Current == null)
            {
                // initialize the environments application instance
                new Application();
            }

            if (Application.Current != null)
            {
                Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            }
        }
    }
}