using System;
using System.Drawing;
using System.Collections.Generic;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

using MonoTouch.CoreGraphics;

using System.IO;

namespace xam
{
	public partial class _048_xamViewController : UIViewController
	{
		#region Constructor - Required Not sure why Find out

		public _048_xamViewController(IntPtr handle) : base(handle)
		{

		}

		#endregion

		// TODO: Shrink font to fit in box as text is longer.
		// TODO: Add share to twitter/facebook (learn that integration)
		// TODO: Challenges, etc.

		#region Fields

		bool gameOver = true;

		int?[,] board = new int?[4, 4];
		UILabel[,] boardTiles = new UILabel[4, 4];
		List<NewTile> newTiles = new List<NewTile>();
		List<SlideTile> slideAndCombineTiles = new List<SlideTile>();

		int score = 0;
		int highScore = 0;

		List<HighScore> highScores;

		#endregion

		class HighScore
		{
			public int Score;
			public DateTime Date;

			static string filePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments) + "HighScores.md";

			public static List<HighScore> LoadScores(ref int highScore)
			{
				var list = new List<HighScore>();

				if (File.Exists(filePath))
				{
					string[] lines = File.ReadAllLines(filePath);
					foreach (var line in lines)
					{
						var chunks = line.Split(',');
						list.Add(new HighScore() {
							Score = int.Parse(chunks [0]),
							Date = DateTime.Parse(chunks [1])
						});
					}

					list.Sort((x, y) => x.Score.CompareTo(y.Score));

					highScore = list [0].Score;
				}

				return list;
			}

			public static void SaveHighScore(int score, DateTime date, List<HighScore> scores)
			{
				using (var fd = new StreamWriter(filePath))
				{
					foreach (var s in scores)
					{
						fd.WriteLine(s.Score + "," + s.Date.ToShortDateString());
					}

					fd.WriteLine(score + "," + date.ToShortDateString());
				}
			}
		}

		Dictionary<int, ColorPair> tileColors = new Dictionary<int, ColorPair>();

		class ColorPair
		{
			public CGColor BackColor;
			public CGColor TextColor;

			public ColorPair(CGColor backColor, CGColor textColor)
			{
				this.BackColor = backColor;
				this.TextColor = textColor;
			}
		}

		private void SetupTileColors()
		{
			tileColors.Add(2, new ColorPair(new CGColor(0.93f, 0.93f, 0.93f, 1), new CGColor(0, 0, 0, 1)));
			tileColors.Add(4, new ColorPair(new CGColor(0.80f, 0.80f, 0.82f, 1), new CGColor(0, 0, 0, 1)));
			tileColors.Add(8, new ColorPair(new CGColor(0.95f, 0.74f, 0.37f, 1), new CGColor(1, 1, 1, 1)));
			tileColors.Add(16, new ColorPair(new CGColor(0.96f, 0.67f, 0.20f, 1), new CGColor(1, 1, 1, 1)));
			tileColors.Add(32, new ColorPair(new CGColor(0.99f, 0.55f, 0.51f, 1), new CGColor(1, 1, 1, 1)));
			tileColors.Add(64, new ColorPair(new CGColor(0.99f, 0.35f, 0.18f, 1), new CGColor(1, 1, 1, 1)));
			tileColors.Add(128, new ColorPair(new CGColor(0.95f, 0.87f, 0.40f, 1), new CGColor(1, 1, 1, 1)));
			tileColors.Add(256, new ColorPair(new CGColor(1.00f, 0.92f, 0.43f, 1), new CGColor(1, 1, 1, 1)));
			tileColors.Add(512, new ColorPair(new CGColor(0.99f, 0.15f, 0.17f, 1), new CGColor(1, 1, 1, 1)));
			tileColors.Add(1024, new ColorPair(new CGColor(1.00f, 0.91f, 0.58f, 1), new CGColor(1, 1, 1, 1)));
			tileColors.Add(2048, new ColorPair(new CGColor(0.52f, 0.99f, 0.39f, 1), new CGColor(1, 1, 1, 1)));
			tileColors.Add(4096, new ColorPair(new CGColor(0.36f, 0.60f, 0.99f, 1), new CGColor(1, 1, 1, 1)));
			tileColors.Add(8196, new ColorPair(new CGColor(0.99f, 0.42f, 0.95f, 1), new CGColor(1, 1, 1, 1)));
			tileColors.Add(16384, new ColorPair(new CGColor(0.43f, 1.00f, 0.90f, 1), new CGColor(1, 1, 1, 1)));
			tileColors.Add(32768, new ColorPair(new CGColor(0.24f, 0.99f, 0.49f, 1), new CGColor(1, 1, 1, 1)));

		}

		#region View lifecycle

		public override void ViewWillAppear(bool animated)
		{
			base.ViewWillAppear(animated);

			SetupTileColors();

			gameOver = false;

			DrawBlankBoard();

			RandomPlacement(-1, -1, -1);
			RandomPlacement(-1, -1, -1);
//			RandomPlacement (0,0,2);
//			RandomPlacement (0,1,2);
//			RandomPlacement (0,2,2);
//			RandomPlacement (0,3,2);

			Animations();

			highScores = HighScore.LoadScores(ref highScore);

			UpdateScoreLabels();
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

			switch (recognizer.Direction)
			{
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

			RandomPlacement(-1, -1, -1);
			Animations();

			UpdateScoreLabels();

			if (CheckGameOver())
			{
				// TODO: Direct to a new screen, with score and try again buttons.
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

			// TODO: Add logic to test if combines are possible.

			return true;
		}

		#endregion

		#region MoveGeneric + LeftRightUpDown

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
			for (int row = start; limitCheck(row, end); row = IncOrDec(incOrDec, row, 1))
			{
				for (int col = start; limitCheck(col, end); col = IncOrDec(incOrDec, col, 1))
				{
					int i = 1;
					while (!boardAccess(row, col).HasValue && limitCheck(IncOrDec(incOrDec, col, i), end))
					{
						if (boardAccess(row, IncOrDec(incOrDec, col, i)).HasValue)
						{
							boardSetter(row, col, boardAccess(row, IncOrDec(incOrDec, col, i)));
							boardSetter(row, IncOrDec(incOrDec, col, i), null);

							var tile = new SlideTile();
							setTileCoords(incOrDec, row, col, i, tile);
							slideAndCombineTiles.Add(tile);
						}
						i++;
					}

					i = 1;
					while (boardAccess(row, col).HasValue
					       && limitCheck(IncOrDec(incOrDec, col, i), end)
					       && !boardAccess(row, IncOrDec(incOrDec, col, i)).HasValue)
					{
						i++;
					}

					if (boardAccess(row, col).HasValue
					    && limitCheck(IncOrDec(incOrDec, col, i), end)
					    && boardAccess(row, col) == boardAccess(row, IncOrDec(incOrDec, col, i)))
					{
						boardSetter(row, col, boardAccess(row, col) * 2);
						boardSetter(row, IncOrDec(incOrDec, col, i), null);

						var tile = new CombineTile();
						setTileCoords(incOrDec, row, col, i, tile);
						tile.NewValue = boardAccess(row, col).Value;
						slideAndCombineTiles.Add(tile);

						// NOTE: WOOOOOOOOOO!!!
						score += tile.NewValue;
					}
				}
			}
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

			int tests = 0;

			// allow manual override for testing.
			if (row < 0)
			{
				do
				{
					row = (r.Next() % 4);
					col = (r.Next() % 4);
					tests++;
				} while (board [row, col].HasValue && tests < 16);
			}

			// we might let them play with a full board if swipes are possible, but not be able to place squares.
			if (tests <= 16)
			{
				var newValue = val > 0 ? val : (r.Next() % 2 + 1) * 2;

				board [row, col] = newValue;

				newTiles.Add(new NewTile() {
					ToRow = row,
					ToCol = col
				});

				// official 2048 app only gives points for combined tiles.
				// score += newValue;
			}
		}

		#endregion

		#region RunAnimations

		void SetTileColor(int value, UILabel label)
		{
			var col = tileColors [value];
			label.Layer.BackgroundColor = col.BackColor;
			label.TextColor = new UIColor(col.TextColor);
		}

		void SlideAndCombineTiles()
		{
			foreach (var slide in slideAndCombineTiles)
			{

				if (slide is CombineTile)
				{
					var label = boardTiles [slide.ToRow, slide.ToCol];

					SetTileColor(board [slide.ToRow, slide.ToCol].Value, label);

					label.Text = (slide as CombineTile).NewValue.ToString();

					boardTiles [slide.FromRow, slide.FromCol].RemoveFromSuperview();
					boardTiles [slide.FromRow, slide.FromCol] = null;
				} else
				{
					var label = boardTiles [slide.FromRow, slide.FromCol];
			
					//SetTileColor(board[slide.ToRow, slide.ToCol].Value, label);

					label.Frame = GetSquareFrame(slide.ToRow, slide.ToCol);

					boardTiles [slide.ToRow, slide.ToCol] = label;
					boardTiles [slide.FromRow, slide.FromCol] = null;
				}
			}

			slideAndCombineTiles.Clear();
		}

		void NewTiles()
		{
			foreach (var nta in newTiles)
			{
				UILabel l = DrawSquare(nta.ToRow, nta.ToCol, board [nta.ToRow, nta.ToCol].ToString(), null);

				SetTileColor(board [nta.ToRow, nta.ToCol].Value, l);

				// starts tiny
				l.Transform = CGAffineTransform.MakeScale(.2f, .2f);
				UIView.Animate(0.1f,
					() =>
					{
						//bigger
						l.Transform = CGAffineTransform.MakeScale(1.25f, 1.25f);
					}, 
					() =>
					{
						UIView.Animate(0.2f, 
							() =>
							{
								//then regular size
								l.Transform = CGAffineTransform.MakeScale(1, 1);
							});
					}
				);
				boardTiles [nta.ToRow, nta.ToCol] = l;
			}
		
			newTiles.Clear();
		}

		private readonly object animeLockObject = new object();

		public void Animations()
		{
			lock (animeLockObject)
			{
				UIView.Animate(.2f,
					() =>
					{
						SlideAndCombineTiles();
						//SlideTiles();
					},
					() =>
					{
						NewTiles();
					}
				);
			}
		}

		#endregion

		void UpdateScoreLabels()
		{
			labelScore.Text = score.ToString();

			if (score > highScore)
			{
				highScore = score;
				HighScore.SaveHighScore(score, DateTime.Today, highScores);
			}

			labelBest.Text = highScore.ToString();
		}

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
			l2.Font = UIFont.FromName(@"Futura", 20);
			l2.Text = value.ToString();

			//l2.BackgroundColor = UIColor.Yellow;
			l2.Layer.CornerRadius = 4;

			if (value != "")
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
			boardView.Layer.CornerRadius = 5;
			boardView.Layer.BackgroundColor = new CGColor(0.7f, 0.7f, 0.7f, 1);

			for (int row = 0; row < 4; row++)
			{
				for (int col = 0; col < 4; col++)
				{
					DrawSquare(row, col, "", new UIColor(0.77f, 0.77f, 0.77f, 1));
				}
			}
		}

		#endregion

		#region DebugBoard

		public string DebugBoard()
		{
			string debug = "";

			for (int i = 0; i < 4; i++)
			{
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


