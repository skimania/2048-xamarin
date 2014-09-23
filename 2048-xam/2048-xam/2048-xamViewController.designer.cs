// WARNING
//
// This file has been generated automatically by Xamarin Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using System;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.CodeDom.Compiler;

namespace xam
{
	[Register ("_048_xamViewController")]
	partial class _048_xamViewController
	{
		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIView boardView { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UILabel labelBest { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UILabel labelScore { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UILabel labelTitle { get; set; }

		void ReleaseDesignerOutlets ()
		{
			if (boardView != null) {
				boardView.Dispose ();
				boardView = null;
			}
			if (labelBest != null) {
				labelBest.Dispose ();
				labelBest = null;
			}
			if (labelScore != null) {
				labelScore.Dispose ();
				labelScore = null;
			}
			if (labelTitle != null) {
				labelTitle.Dispose ();
				labelTitle = null;
			}
		}
	}
}
