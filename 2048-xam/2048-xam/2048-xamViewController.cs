using System;
using System.Drawing;
using System.Collections.Generic;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

using MonoTouch.CoreGraphics;

using System.IO;
using System.Linq;
using System.Threading;

namespace xam
{
	public partial class _048_xamViewController : UIViewController
	{
		#region Constructor - Required Not sure why Find out

		public _048_xamViewController(IntPtr handle) : base(handle)
		{

		}

		#endregion`

		// TODO: Shrink font to fit in box as text is longer.
		// TODO: Add share to twitter/facebook (learn that integration)
		// TODO: Challenges, etc.

		/* **************************************
		 * TODO:
		 * 
		 * Convert to Poker Game Project.
		 * 
		 * //1. Make board 5x5
		 * //2. Render cards instead of numbers
		 * 3. Change slide and combine logic as follows:
		 * 		a. No more combining
		 * 		b. After sliding test complete rows for poker hands (cards in two rows count twice and double score)
		 * 		c. Jokers wild.
		 * 		d. Preview of next 2 cards?
		 * 		e. "Magic" earned after XXX to allow moving cards, sorting cards, etc.
		 * 4. Update scoring logic to score value of hands
		 * 
		 * **************************************/

		#region Fields

		bool gameOver = true;

		Card[,] cardsOnBoard = new Card[5, 5];
		UIView[,] boardTiles = new UIView[5, 5];
		List<NewTile> newTiles = new List<NewTile>();
		List<SlideTile> slideTiles = new List<SlideTile>();

		int score = 0;
		int highScore = 0;

		List<HighScore> highScores;

		#endregion

		#region HighScore Class

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

		#endregion

		#region Card/Suit/Rank Classes and Enum

		public enum Suit
		{
			Diamond,
			Club,
			Heart,
			Spade
		}

		public enum Rank
		{
			_2 = 2,
			_3,
			_4,
			_5,
			_6,
			_7,
			_8,
			_9,
			_10,
			Jack,
			Queen,
			King,
			Ace
		}

		public class Card
		{
			public Suit Suit;
			public Rank Rank;

			public override string ToString()
			{
				return Rank.ToString().Replace("_", "") + " of " + Suit;
			}
		}

		#endregion

		#region View lifecycle

		public override void ViewWillAppear(bool animated)
		{
			base.ViewWillAppear(animated);

			//SetupTileColors();

			gameOver = false;

			DrawBlankBoard();

			RandomPlacement(-1, -1, null);
			RandomPlacement(-1, -1, null);
			RandomPlacement(-1, -1, null);
			RandomPlacement(-1, -1, null);
			RandomPlacement(-1, -1, null);
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

			RandomPlacement(-1, -1, null);
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
			for (int i = 0; i < 5; i++)
				for (int j = 0; j < 5; j++)
					if (cardsOnBoard [i, j] == null)
						return false;

			// TODO: Add logic to test if combines are possible.

			return true;
		}

		#endregion

		#region MoveGeneric + LeftRightUpDown

		void MoveLeft()
		{
			MoveGeneric(
				(i, j) => cardsOnBoard [i, j],
				(i, j, k) => cardsOnBoard [i, j] = k,
				true, 
				(i, lim) => i < lim, 
				0, 5, 
				SetTileCoordsH);
		}

		void MoveRight()
		{
			MoveGeneric(
				(i, j) => cardsOnBoard [i, j],
				(i, j, k) => cardsOnBoard [i, j] = k,
				false, 
				(i, lim) => i > lim, 
				4, -1,
				SetTileCoordsH);
				
		}

		void MoveUp()
		{
			MoveGeneric(
				(i, j) => cardsOnBoard [j, i],
				(i, j, k) => cardsOnBoard [j, i] = k,
				true, 
				(i, lim) => i < lim, 
				0, 5,
				SetTileCoordsV);
		}

		void MoveDown()
		{
			MoveGeneric(
				(i, j) => cardsOnBoard [j, i],
				(i, j, k) => cardsOnBoard [j, i] = k,
				false, 
				(i, lim) => i > lim, 
				4, -1,
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

		void MoveGeneric(Func<int, int, Card> boardAccess, Action<int, int, Card> boardSetter, bool incOrDec, 
		                 Func<int, int, bool> limitCheck, int start, int end,
		                 Action<bool, int, int, int, SlideTile> setTileCoords)
		{
			for (int row = start; limitCheck(row, end); row = IncOrDec(incOrDec, row, 1))
			{
				for (int col = start; limitCheck(col, end); col = IncOrDec(incOrDec, col, 1))
				{
					int i = 1;
					while (boardAccess(row, col) == null && limitCheck(IncOrDec(incOrDec, col, i), end))
					{
						if (boardAccess(row, IncOrDec(incOrDec, col, i)) != null)
						{
							boardSetter(row, col, boardAccess(row, IncOrDec(incOrDec, col, i)));
							boardSetter(row, IncOrDec(incOrDec, col, i), null);

							var tile = new SlideTile();
							setTileCoords(incOrDec, row, col, i, tile);
							slideTiles.Add(tile);
						}
						i++;
					}

					/*

					// We are no longer interested in combining cards.

					i = 1;
					while (boardAccess(row, col).HasValue
					       && limitCheck(IncOrDec(incOrDec, col, i), end)
					       && !boardAccess(row, IncOrDec(incOrDec, col, i)).HasValue) {
						i++;
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

						// NOTE: WOOOOOOOOOO!!!
						score += tile.NewValue;
					}
					*/
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

		/*
		public class CombineTile : SlideTile
		{
			public int NewValue = 0;
		}
		*/

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

		void RandomPlacement(int row, int col, Card testCard)
		{
			int tests = 0;

			// allow manual override for testing.
			if (row < 0)
			{
				do
				{
					row = (r.Next() % 5);
					col = (r.Next() % 5);
					tests++;
				} while (cardsOnBoard [row, col] != null && tests < 25);
			}

			// we might let them play with a full board if swipes are possible, but not be able to place squares.
			if (tests <= 25)
			{
				var newValue = new Card() {
					Suit = (Suit)(r.Next() % 4),// Suit.Club,
					Rank = (Rank)(r.Next() % 13 + 2)
				};//val > 0 ? val : (r.Next() % 2 + 1) * 2;

				cardsOnBoard [row, col] = newValue;

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

		void SlideAndCombineTiles()
		{
			foreach (var slide in slideTiles)
			{
				var label = boardTiles [slide.FromRow, slide.FromCol];
		
				label.Frame = GetSquareFrame(slide.ToRow, slide.ToCol);

				boardTiles [slide.ToRow, slide.ToCol] = label;
				boardTiles [slide.FromRow, slide.FromCol] = null;
			}

			slideTiles.Clear();
		}

		#region Hand - Check Hand Logic

		class Hand
		{
			public int? Row;
			public int? Col;
			public Card[] Cards;

			public Hand(Card[] cards)
			{
				this.Cards = cards;
			}

			public bool TestGood()
			{
				return HasPair() ||
				HasTwoPair() ||
				HasThree() ||
				HasFour() ||
				HasFlush() ||
				HasFullHouse() ||
				HasStraight();
			}

			public bool HasPair()
			{
				var rankGroups = Cards.GroupBy(card => card.Rank);
				return rankGroups.FirstOrDefault(grp => grp.Count() == 2) != null;
			}

			public bool HasTwoPair()
			{
				var rankGroups = Cards.GroupBy(card => card.Rank).ToList();
				var pairA = rankGroups.FirstOrDefault(grp => grp.Count() == 2);

				if (pairA == null)
					return false;

				rankGroups.Remove(pairA);

				var pairB = rankGroups.FirstOrDefault(grp => grp.Count() == 2);

				return pairB != null;
			}

			public bool HasThree()
			{
				var rankGroups = Cards.GroupBy(card => card.Rank);
				return rankGroups.FirstOrDefault(grp => grp.Count() == 3) != null;
			}

			public bool HasFour()
			{
				var rankGroups = Cards.GroupBy(card => card.Rank);
				return rankGroups.FirstOrDefault(grp => grp.Count() == 4) != null;
			}

			public bool HasFullHouse()
			{
				return HasPair() && HasThree();
			}

			public bool HasFlush()
			{
				var rankGroups = Cards.GroupBy(card => card.Suit);
				return rankGroups.FirstOrDefault(grp => grp.Count() == 5) != null;
			}

			public bool HasStraight()
			{
				var sorted = Cards.OrderBy(c => c.Rank).ToArray();
				for (int i = 0; i < 4; i++)
				{
					if (sorted [i].Rank != sorted [i + 1].Rank)
						return false;
				}
				return true;
			}

			public bool HasStraighFlush()
			{
				return HasStraight() && HasFlush();
			}

			public bool HasRoyalStraighFlush()
			{
				return HasStraighFlush() && HighCard().Rank == Rank.Ace;
			}

			public Card HighCard()
			{
				return Cards.OrderBy(c => c.Rank).ThenBy(c => c.Suit).First();
			}
		}

		#endregion

		#region GetAllHands()

		List<Hand> GetAllHands()
		{
			List<Hand> handsToCheck = new List<Hand>();
			for (int i = 0; i < 5; i++)
			{
				Card[] handRow = new Card[5];
				Card[] handCol = new Card[5];
				for (int j = 0; j < 5; j++)
				{
					if (handRow != null)
					{
						handRow [j] = cardsOnBoard [i, j];
						if (handRow [j] == null)
							handRow = null;
					}
					if (handCol != null)
					{
						handCol [j] = cardsOnBoard [j, i];
						if (handCol [j] == null)
							handCol = null;
					}
				}
				if (handRow != null)
					handsToCheck.Add(new Hand(handRow) {
						Row = i
					});
				if (handCol != null)
					handsToCheck.Add(new Hand(handCol) {
						Col = i
					});
			}
			return handsToCheck;
		}

		#endregion


		void ClearRows()
		{
			var handsToCheck = GetAllHands();

			List<UIView> highlights = new List<UIView>();

			foreach (var hand in handsToCheck.ToList())
			{
				if (hand.TestGood())
				{
					labelStatus.Text = "Woooooo";

					if (hand.Row.HasValue)
						HighlightRow(hand.Row.Value);

					if (hand.Col.HasValue)
						HighlightCol(hand.Col.Value);
				} else
				{
					handsToCheck.Remove(hand);
				}
			}

			foreach (var hand in handsToCheck)
			{
				for (int i = 0; i < 5; i++)
				{
					UIView tile = boardTiles [hand.Row ?? i, hand.Col ?? i];
					cardsOnBoard [hand.Row ?? i, hand.Col ?? i] = null;
					UIView.Animate(1, () => tile.RemoveFromSuperview());
				}
			}
		}

		UIView HighlightRow(int row)
		{
			UIView highlightbar = new UIView(new RectangleF(15, 15 + 64 * row, 250, 64));
			HighlightLine(highlightbar);
			return highlightbar;
		}

		UIView HighlightCol(int col)
		{
			UIView highlightbar = new UIView(new RectangleF(15 + 50 * col, 15, 50, 320));
			HighlightLine(highlightbar);
			return highlightbar;
		}

		void HighlightLine(UIView highlightbar)
		{
			highlightbar.Layer.BackgroundColor = new CGColor(1, 0, 0, 0);

			highlightbar.Layer.BorderColor = ColorHelper.ConvertUIColorToCGColor(UIColor.Black);
			highlightbar.Layer.BorderWidth = 2;
			highlightbar.Layer.CornerRadius = 3;

			boardView.Add(highlightbar);

			UIView.Animate(2f, () =>
			{
				highlightbar.Layer.BackgroundColor = new CGColor(1, 0, 0, 0.2f);
			} 
			);

			ChainAnimations(0,
				new ChainAnimationOption() {
					duration = 1,
					action = () =>
					{
						highlightbar.Layer.BackgroundColor = new CGColor(1, 0, 0, 0.2f);

					}
				},
				new ChainAnimationOption() {
					duration = 1,
					action = () =>
					{
						highlightbar.Layer.BackgroundColor = new CGColor(0, 1, 0, 0.2f);

					}
				},
				new ChainAnimationOption() {
					duration = 1,
					action = () =>
					{
						highlightbar.Layer.BackgroundColor = new CGColor(0, 0, 1, 0.2f);

					}
				}
			);

			//t.Wait();


		}

		public class ChainAnimationOption
		{
			public float duration = 1;
			public NSAction action;
		}

		private void ChainAnimations(int i = 0, params ChainAnimationOption[] animations)
		{
			if (i < animations.Length)
				UIView.Animate(animations [i].duration, animations [i].action, () =>
				{
					i++;
					ChainAnimations(i, animations);
				}
				);
		}



		void NewTiles()
		{
			foreach (var nta in newTiles)
			{
				var l = DrawSquare(nta.ToRow, nta.ToCol, cardsOnBoard [nta.ToRow, nta.ToCol], null);

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
					},
					() =>
					{
						UIView.Animate(.2f,
							() =>
							{
								NewTiles();
							},
							() =>
							{
								ClearRows();
							}
						);
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
			return new RectangleF(20 + 50 * col, 20 + 64 * row, 40, 54);
		}

		public UIView DrawSquare(int row, int col, Card card, UIColor backColor)
		{
			UIView l2 = new UIView(GetSquareFrame(row, col));

			l2.Layer.BackgroundColor = ColorHelper.ConvertUIColorToCGColor(backColor ?? UIColor.White);
			//l2.Layer.AllowsEdgeAntialiasing = true;
			l2.Layer.CornerRadius = 4;

			if (card != null)
			{
				UIImageView img = new UIImageView(
					                  UIImage.FromBundle(card.Suit.ToString().ToLower() + ".png"));


				UILabel lbl = new UILabel(new RectangleF(0, 0, 40, 54));

				lbl.Text = card.Rank.ToString().Replace("_", "").Replace("ack", "").Replace("ueen", "").Replace("ing", "").Replace("ce", "");// value.ToString();

				lbl.AdjustsFontSizeToFitWidth = true;
				lbl.Lines = 1;

				lbl.TextAlignment = UITextAlignment.Center;
				lbl.TextColor = UIColor.White;
				lbl.Font = UIFont.FromName(@"Futura", 20);


				l2.Layer.BorderColor = ColorHelper.ConvertUIColorToCGColor(UIColor.Black);
				l2.Layer.BorderWidth = 2;

				l2.AddSubview(img);
				l2.AddSubview(lbl);
			}

			boardView.Add(l2);

			return l2;
		}

		#endregion

		#region DrawBlankBoard

		private void DrawBlankBoard()
		{
			boardView.Layer.CornerRadius = 5;
			boardView.Layer.BackgroundColor = new CGColor(0.7f, 0.7f, 0.7f, 1);

			for (int row = 0; row < 5; row++)
			{
				for (int col = 0; col < 5; col++)
				{
					DrawSquare(row, col, null, new UIColor(0.77f, 0.77f, 0.77f, 1));
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
					debug += cardsOnBoard [i, j] + "|";
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


