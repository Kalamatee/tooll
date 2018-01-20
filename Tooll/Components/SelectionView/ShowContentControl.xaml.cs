﻿// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Windows;
using System.Windows.Input;
using Framefield.Core;
using Framefield.Core.OperatorPartTraits;
using Framefield.Core.Profiling;
using Framefield.Tooll.Components.SelectionView.ShowScene.CameraInteraction;
using Framefield.Tooll.Rendering;

namespace Framefield.Tooll.Components.SelectionView
{



    /** A UserControl that handles rendering and interacting with different Scene, Mesh and Image-Content */
    public partial class ShowContentControl
    {
        public ShowContentControl()
        {
            Loaded += OnLoadedHandler;
            Unloaded += OnUnloadedHandler;
            InitializeComponent();

            RenderConfiguration = new ContentRendererConfiguration()
            {
                ShowGridAndGizmos = true
            };
        }


        private void OnLoadedHandler(object sender, RoutedEventArgs e)
        {
            App.Current.MainWindow.GotFocus += MainWindow_GotFocusHandler;

            // Not sure why this callback is required. Probably to prevent some render update after comming back from full-screen?
            App.Current.MainWindow.ContentRendered += MainWindow_ContentRendered_Handler;

            App.Current.UpdateAfterUserInteractionEvent += App_UpdateAfterUserInteractionHandler;

            KeyDown += KeyDown_Handler;
            KeyUp += KeyUp_Handler;
            LostFocus += LostFocus_Handler;

            App.Current.CompositionTargertRenderingEvent += App_CompositionTargertRenderingHandler;
            App.Current.UpdateRequiredAfterUserInteraction = true;

            _contentRenderer = new ContentRenderer(RenderConfiguration);
            SetupRenderer();

            CameraInteraction = new CameraInteraction(this);     // Note: This requires ShowSceneControl to have been loaded
        }


        private void OnUnloadedHandler(object sender, RoutedEventArgs e)
        {
            KeyDown -= KeyDown_Handler;
            KeyUp -= KeyUp_Handler;

            App.Current.MainWindow.GotFocus -= MainWindow_GotFocusHandler;
            App.Current.MainWindow.ContentRendered -= MainWindow_ContentRendered_Handler;

            LostFocus -= LostFocus_Handler;

            if (App.Current != null)
            {
                App.Current.UpdateAfterUserInteractionEvent -= App_UpdateAfterUserInteractionHandler;
                App.Current.CompositionTargertRenderingEvent -= App_CompositionTargertRenderingHandler;

                CameraInteraction.Discard();
            }
            _contentRenderer.CleanUp();
        }


        #region Event-Handlers
        private void MainWindow_GotFocusHandler(object sender, RoutedEventArgs e)
        {
            _contentRenderer.Reinitialize();
        }


        private void MouseDown_Handler(object sender, MouseButtonEventArgs e)
        {
            Focus();
            CameraInteraction.HandleMouseDown(e.ChangedButton);

            Mouse.Capture(this);
            e.Handled = true;
        }


        private void MouseUp_Handler(object sender, MouseButtonEventArgs e)
        {
            var wasRightMouseClick = CameraInteraction.HandleMouseUp(e.ChangedButton);


            // Release captured mouse
            if (!CameraInteraction.AnyMouseButtonPressed())
                Mouse.Capture(null);

            if (!wasRightMouseClick)
                e.Handled = true;
        }


        private void MouseWheel_Handler(object sender, MouseWheelEventArgs e)
        {
            CameraInteraction.HandleMouseWheel(e.Delta);
            e.Handled = true;
        }


        private void LostFocus_Handler(object sender, RoutedEventArgs e)
        {
            CameraInteraction.HandleFocusLost();
        }


        private void KeyDown_Handler(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!e.IsRepeat)
            {
                CameraInteraction.HandleKeyDown(e);
            }
        }


        private void KeyUp_Handler(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (RenderConfiguration.Operator == null)
                return;

            if (e.Key == Key.Return || Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
                SwitchToFullscreenMode();

            if (CameraInteraction.HandleKeyUp(e))
                return;
        }


        private void MainWindow_ContentRendered_Handler(object sender, EventArgs e)
        {
            _contentRenderer.Reinitialize();
        }


        private void SizeChanged_Handler(object sender, SizeChangedEventArgs e)
        {

            if (_contentRenderer != null)
            {
                SetRendererSizeFromWindow();
                _contentRenderer.Reinitialize();
            }
        }


        void App_UpdateAfterUserInteractionHandler(object sender, EventArgs e)
        {
            RenderContent();
        }


        private void App_CompositionTargertRenderingHandler(object source, EventArgs e)
        {
            if (CameraInteraction == null || !CameraInteraction.UpdateAndCheckIfRedrawRequired())
                return;

            if (CurrentOpIsACamera)
            {
                App.Current.UpdateRequiredAfterUserInteraction = true;
            }
            else
            {
                RenderContent();
            }
        }


        private bool CurrentOpIsACamera
        {
            get
            {
                return
                    RenderConfiguration.Operator != null
                    && RenderConfiguration.Operator.InternalParts.Count > 0
                    && RenderConfiguration.Operator.InternalParts[0].Func is ICameraProvider;
            }
        }
        #endregion



        public void SetOperatorAndOutputIndex(Operator op, int outputIndex = 0)
        {
            if (op == null || op.Outputs.Count < outputIndex + 1)
                return;

            RenderConfiguration.ShownOutputIndex = outputIndex;
            RenderConfiguration.Operator = op;
            //_operator = op;
            RenderContent();
        }


        #region Rendering
        private void SwitchToFullscreenMode()
        {
            var fsView = new FullScreenView(_contentRenderer.RenderSetup);
        }

        private void RenderContent()
        {
            if (!IsVisible)
                return;

            if (TimeLoggingSourceEnabled)
                TimeLogger.BeginFrame(App.Current.Model.GlobalTime);

            _contentRenderer.RenderContent();

            D3DDevice.EndFrame();
            if (TimeLoggingSourceEnabled)
                TimeLogger.EndFrame();
        }


        private void SetupRenderer()
        {
            SetRendererSizeFromWindow();
            _contentRenderer.SetupRendering();
            XSceneImage.Source = _contentRenderer.D3DImageContainer;
        }

        private void SetRendererSizeFromWindow()
        {
            RenderConfiguration.Width = (int)XGrid.ActualWidth;
            RenderConfiguration.Height = (int)XGrid.ActualHeight;
        }


        //public bool ShowGridAndGizmos { get; set; }
        //public bool RenderWithGammaCorrection { get; set; }
        //public int PreferredCubeMapSideIndex { get; set; }

        /** After rendering a image this flag can be used to display UI-elements relevant for CubeMaps */
        public bool RenderedImageIsACubemap
        {
            get
            {
                return _contentRenderer.RenderedImageIsACubemap;
            }
        }

        //public Render

        ContentRenderer _contentRenderer;


        #endregion

        //private Operator _operator;
        public D3DRenderSetup RenderSetup { get { return _contentRenderer.RenderSetup; } }
        public ContentRendererConfiguration RenderConfiguration;

        public bool TimeLoggingSourceEnabled { get; set; }

        public CameraInteraction CameraInteraction { get; set; }
    }
}