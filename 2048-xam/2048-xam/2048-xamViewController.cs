using System;
using System.Drawing;
using System.Collections.Generic;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

using MonoTouch.CoreGraphics;

namespace xam
{
	public partial class _048_xamViewController : UIViewController
	{
		#region Constructor - Required Not sure why Find out

		public _048_xamViewController(IntPtr handle) : base(handle)
		{

		}

		#endregion`

		#region Fields

		bool gameOver = true;

		int?[,] board = new int?[4, 4];
		UILabel[,] boardTiles = new UILabel[4, 4];
		List<NewTile> newTiles = new List<NewTile>();
		List<SlideTile> slideAndCombineTiles = new List<SlideTile>();
		//List<CombineTile> combineTiles = new List<CombineTile>();

		#endregion

		#region View lifecycle

		public override void ViewWillAppear(bool animated)
		{
			base.ViewWillAppear(animated);

			gameOver = false;

			DrawBlankBoard();

			RandomPlacement(-1, -1, -1);
			RandomPlacement(-1, -1, -1);
//			RandomPlacement (0,0,2);
//			RandomPlacement (0,1,2);
//			RandomPlacement (0,2,2);
//			RandomPlacement (0,3,2);
			Animations();

		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();

			View.AddGestureRecognizer(new UISwipeGestureRecognizer(new Action<UISwipeGestureRecognizer>(SwipeHandler)) {
				Direction = UISwipeGestureRecognizerDirection.Left
			});
			View.AddGestureRecognizer(new UISwipeGestureRecognizer(new Action<UISwipeGestureRecognizer>(SwipeHandler)) {
				Direction = UISwipeGestureRecognizerDirection.Right
			});
			View.AddGestureRecognizer(new UISwipeGestureRecognizer(new Action<UISwipeGestureRecognizer>(SwipeHandler)) {
				Direction = UISwipeGestureRecognizerDirection.Up
			});
			View.AddGestureRecognizer(new UISwipeGestureRecognizer(new Action<UISwipeGestureRecognizer>(SwipeHandler)) {
				Direction = UISwipeGestureRecognizerDirection.Down

			});
		}

		#endregion

		#region SwipeHandler

		void SwipeHandler(UISwipeGestureRecognizer recognizer)
		{

			if (gameOver)
				return;

			labelScore.Text = recognizer.Direction.ToString();


			switch (recognizer.Direction) {
			case UISwipeGestureRecognizerDirection.Left:
				MoveLeft();
				break;
			case UISwipeGestureRecognizerDirection.Right:
				MoveRight();
				break;
			case UISwipeGestureRecognizerDirection.Up:
				MoveUp();
				break;
			case UISwipeGestureRecognizerDirection.Down:
				MoveDown();
				break;
			}

			//MoveLeftTest();

			RandomPlacement(-1, -1, -1);
			Animations();

			//labelDebug.Text = DebugBoard();

			if (CheckGameOver()) {
				labelScore.Text = "Game Over";
				gameOver = true;
			}
		}

		bool CheckGameOver()
		{
			for (int i = 0; i < 4; i++)
				for (int j = 0; j < 4; j++)
					if (!board [i, j].HasValue)
						return false;
			return true;
		}

		#endregion

		#region MoveGeneric + LFUD

		void MoveLeft()
		{
			MoveGeneric(
				(i, j) => board [i, j],
				(i, j, k) => board [i, j] = k,
				true, 
				(i, lim) => i < lim, 
				0, 4, 
				SetTileCoordsH);
		}

		void MoveRight()
		{
			MoveGeneric(
				(i, j) => board [i, j],
				(i, j, k) => board [i, j] = k,
				false, 
				(i, lim) => i > lim, 
				3, -1,
				SetTileCoordsH);
				
		}

		void MoveUp()
		{
			MoveGeneric(
				(i, j) => board [j, i],
				(i, j, k) => board [j, i] = k,
				true, 
				(i, lim) => i < lim, 
				0, 4,
				SetTileCoordsV);
		}

		void MoveDown()
		{
			MoveGeneric(
				(i, j) => board [j, i],
				(i, j, k) => board [j, i] = k,
				false, 
				(i, lim) => i > lim, 
				3, -1,
				SetTileCoordsV);
		}

		static int IncOrDec(bool incOrDec, int row, int amt)
		{
			return incOrDec ? row + amt : row - amt;
		}

		static void SetTileCoordsH(bool incOrDec, int row, int col, int i, SlideTile tile)
		{
			tile.FromRow = row;
			tile.FromCol = IncOrDec(incOrDec, col, i);
			tile.ToRow = row;
			tile.ToCol = col;
		}

		static void SetTileCoordsV(bool incOrDec, int row, int col, int i, SlideTile tile)
		{
			tile.FromRow = IncOrDec(incOrDec, col, i);
			tile.FromCol = row;
			tile.ToRow = col;
			tile.ToCol = row;
		}

		void MoveGeneric(Func<int, int, int?> boardAccess, Action<int, int, int?> boardSetter, bool incOrDec, 
		                 Func<int, int, bool> limitCheck, int start, int end,
		                 Action<bool, int, int, int, SlideTile> setTileCoords)
		{
			for (int row = start; limitCheck(row, end); row = IncOrDec(incOrDec, row, 1)) {
				for (int col = start; limitCheck(col, end); col = IncOrDec(incOrDec, col, 1)) {
					int i = 1;//IncOrDec(incOrDec, start, 1);
					while (!boardAccess(row, col).HasValue && limitCheck(IncOrDec(incOrDec, col, i), end)) {
						if (boardAccess(row, IncOrDec(incOrDec, col, i)).HasValue) {
							boardSetter(row, col, boardAccess(row, IncOrDec(incOrDec, col, i)));
							boardSetter(row, IncOrDec(incOrDec, col, i), null);

							var tile = new SlideTile();
							setTileCoords(incOrDec, row, col, i, tile);
							slideAndCombineTiles.Add(tile);
						}
						i++;// = IncOrDec(incOrDec, i, 1);
					}

					i = 1;//IncOrDec(incOrDec, start, 1);
					while (boardAccess(row, col).HasValue
					       && limitCheck(IncOrDec(incOrDec, col, i), end)
					       && !boardAccess(row, IncOrDec(incOrDec, col, i)).HasValue) {
						i++;//i = IncOrDec(incOrDec, i, 1);
					}

					if (boardAccess(row, col).HasValue
					    && limitCheck(IncOrDec(incOrDec, col, i), end)
					    && boardAccess(row, col) == boardAccess(row, IncOrDec(incOrDec, col, i))) {

						boardSetter(row, col, boardAccess(row, col) * 2);
						boardSetter(row, IncOrDec(incOrDec, col, i), null);

						var tile = new CombineTile();
						setTileCoords(incOrDec, row, col, i, tile);
						tile.NewValue = boardAccess(row, col).Value;
						slideAndCombineTiles.Add(tile);

						// jump ahead
						//col = IncOrDec(incOrDec, col, 1);
					}
				}

			}


			/*
			for (int i = start; limitCheck(i, end); i = incOrDec ? i+1 : i-1) {
				for (int j = start; limitCheck(j, end); j = incOrDec ? j+1 : j-1) {
					if (!boardAccess(i,j).HasValue) {
						for (int nextj = incOrDec ? j + 1 : j - 1; limitCheck(nextj, end); nextj = incOrDec ? nextj+1 : nextj-1) {
							if (boardAccess (i, nextj).HasValue) {
								boardSetter (i, j, boardAccess(i, nextj));
								boardSetter (i, nextj, null);
							}
						}
					}
				}
				for (int j = start; limitCheck(j, incOrDec ? end-1 : end+1); j = incOrDec ? j+1 : j-1) {
					if (boardAccess(i, j).HasValue && boardAccess(i, j) == boardAccess(i, incOrDec ? j+1 : j-1)) {
						boardSetter(i, j, boardAccess(i,j) * 2);
						for (int nextj = incOrDec ? j+1 : j-1; limitCheck(nextj, end); nextj = incOrDec ? nextj+1 : nextj-1) {
							if(limitCheck(nextj, incOrDec ? end-1 : end+1))
								boardSetter(i, nextj, boardAccess(i, incOrDec ? nextj+1 : nextj-1));
							else
								boardSetter(i, nextj, null);
						}
						j=incOrDec ? j+1 : j-1;
					}
				}
			}
			*/
		}

		#endregion

		#region TileClasses

		public class NewTile
		{
			public int ToRow = 0;
			public int ToCol = 0;
		}

		public class SlideTile:NewTile
		{
			public int FromRow = 0;
			public int FromCol = 0;
		}

		public class CombineTile : SlideTile
		{
			public int NewValue = 0;
		}

		#endregion

		/* -- Move Function Developed for one direction first, then converted to a generic version.
		void MoveLeftTest ()
		{
			for (int row = 0; row < 4; row++)
			{
				for (int col = 0; col < 4; col++) 
				{
					int i = 1;
					while (!board [row, col].HasValue && col+i < 4)
					{
						if (board [row, col+i].HasValue) 
						{
							board [row, col] = board [row, col+i];
							board [row, col+i] = null;

							slideAndCombineTiles.Add (new SlideTile () {
								FromRow = row,
								FromCol = col+i,
								ToRow = row,
								ToCol = col
							});
						}
						i++;
					}

					i = 1;
					while (board [row, col].HasValue && col+i < 4 && !board [row, col+i].HasValue)
					{
						i++;
					}

					if (board [row, col].HasValue && col+i < 4 && board [row, col] == board [row, col+i]) 
					{
						slideAndCombineTiles.Add(new CombineTile() {
							FromRow = row,
							FromCol = col + i,
							ToRow = row,
							ToCol = col,
							NewValue = (board [row, col] * 2).Value
						});

						board [row, col] *= 2;
						board [row, col + i] = null;
					}
				}

			}
		}
		*/

		#region RandomPlacement

		Random r = new Random();

		void RandomPlacement(int row, int col, int val)
		{
			//int i = 0;

			//int row;
			//int col;

			// allow manual override for testing.
			if (row < 0) {
				do {
					row = (r.Next() % 4);
					col = (r.Next() % 4);
				} while (board [row, col].HasValue);
			}

			board [row, col] = val > 0 ? val : (r.Next() % 2 + 1) * 2;

			newTiles.Add(new NewTile() {
				ToRow = row,
				ToCol = col
			});
		}

		#endregion

		#region RunAnimations

		void SlideAndCombineTiles()
		{
			foreach (var slide in slideAndCombineTiles) {

				if (slide is CombineTile) {
					var label = boardTiles [slide.ToRow, slide.ToCol];

					label.Layer.BackgroundColor = ColorHelper.ConvertUIColorToCGColor(UIColor.Blue);
					label.Text = (slide as CombineTile).NewValue.ToString();

					//boardTiles[slide.ToRow, slide.ToCol] = label;

					boardTiles [slide.FromRow, slide.FromCol].RemoveFromSuperview();
					boardTiles [slide.FromRow, slide.FromCol] = null;
				} else {
					var label = boardTiles [slide.FromRow, slide.FromCol];
			
					label.Layer.BackgroundColor = ColorHelper.ConvertUIColorToCGColor(UIColor.Red);
					label.Frame = GetSquareFrame(slide.ToRow, slide.ToCol);

					boardTiles [slide.ToRow, slide.ToCol] = label;
					boardTiles [slide.FromRow, slide.FromCol] = null;
				}
			}

			slideAndCombineTiles.Clear();
		}

		/*
		void CombineTiles()
		{
			foreach (var slide in slideAndCombineTiles) {
				var label = boardTiles[slide.ToRow, slide.ToCol];
				label.Layer.BackgroundColor = ColorHelper.ConvertUIColorToCGColor(UIColor.Blue);
				//label.Frame = GetSquareFrame(slide.ToRow, slide.ToCol);
				label.Text = slide.NewValue.ToString();
				boardTiles[slide.ToRow, slide.ToCol] = label;

				boardTiles [slide.FromRow, slide.FromCol].RemoveFromSuperview();
				boardTiles[slide.FromRow, slide.FromCol] = null;
			}

			combineTiles.Clear();
		}
		*/

		void NewTiles()
		{
			foreach (var nta in newTiles) {
				UILabel l = DrawSquare(nta.ToRow, nta.ToCol, board [nta.ToRow, nta.ToCol].ToString(), null);
				// starts tiny
				l.Transform = CGAffineTransform.MakeScale(.2f, .2f);
				UIView.Animate(0.1f,
					() => {
						//bigger
						l.Transform = CGAffineTransform.MakeScale(1.25f, 1.25f);
					}, 
					() => {
						UIView.Animate(0.2f, 
							() => {
								//then regular size
								l.Transform = CGAffineTransform.MakeScale(1, 1);
							});
					}
				);
				boardTiles [nta.ToRow, nta.ToCol] = l;
			}
		
			newTiles.Clear();
		}

		private readonly object animeObject = new object();

		public void Animations()
		{
			lock (animeObject) {
				UIView.Animate(.2f,
					() => {
						SlideAndCombineTiles();
						//SlideTiles();
					},
					() => {
						NewTiles();
					}
				);
			}
		}

		#endregion

		#region DrawSquare

		static RectangleF GetSquareFrame(int row, int col)
		{
			return new RectangleF(20 + 62 * col, 20 + 62 * row, 52, 52);
		}

		public UILabel DrawSquare(int row, int col, string value, UIColor backColor)
		{
			UILabel l2 = new UILabel(GetSquareFrame(row, col));

			l2.TextAlignment = UITextAlignment.Center;
			l2.TextColor = UIColor.Black;
			l2.Font = UIFont.FromName(@"Arial Rounded MT Bold", 20);
			l2.Text = value.ToString();

			//l2.BackgroundColor = UIColor.Yellow;
			l2.Layer.CornerRadius = 4;
			l2.Layer.BorderWidth = 2;
			l2.Layer.BackgroundColor = ColorHelper.ConvertUIColorToCGColor(backColor ?? UIColor.Yellow);
			l2.Layer.BorderColor = ColorHelper.ConvertUIColorToCGColor(UIColor.Black);
			l2.Layer.AllowsEdgeAntialiasing = true;

			boardView.Add(l2);

			return l2;
		}

		#endregion

		#region DrawBlankBoard

		private void DrawBlankBoard()
		{
			for (int row = 0; row < 4; row++) {
				for (int col = 0; col < 4; col++) {
					DrawSquare(row, col, "", UIColor.LightTextColor);
				}
			}
		}

		#endregion

		#region DebugBoard

		public string DebugBoard()
		{
			string debug = "";

			for (int i = 0; i < 4; i++) {
				for (int j = 0; j < 4; j++)
					debug += board [i, j] + "|";
				debug += "\n";
			}

			return debug;
		}

		#endregion
	}

	#region class ColorHelper - Converting UI to CG

	public class ColorHelper
	{
		public static CGColor ConvertUIColorToCGColor(UIColor uiColor)
		{
			float r, g, b, a;
			uiColor.GetRGBA(out r, out g, out b, out a);
			return new CGColor(r, g, b, a);
		}
	}

	#endregion
	
}


