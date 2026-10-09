using Microsoft.Maui.Controls.Shapes;

namespace FitnessApp
{
    // Drag-to-reorder for exercise cards in a StackLayout. Hold a card's handle and drag it:
    // the card you picked up fades out, and a gray dashed slot shows exactly where it will land
    // (above or below the card under your finger). Let go to drop it there.
    // While dragging, the page can collapse its cards (setCompact) so every exercise fits on screen.
    public class CardReorder
    {
        private readonly Layout _container;
        private readonly Action<bool> _setCompact;
        private readonly Action _onReordered;
        private readonly Border _placeholder;
        private readonly Label _placeholderLabel;
        private Frame _dragged;

        public CardReorder(Layout container, Action<bool> setCompact, Action onReordered = null)
        {
            _container = container;
            _setCompact = setCompact;
            _onReordered = onReordered;

            _placeholderLabel = new Label
            {
                TextColor = Theme.TextSecondary,
                FontAttributes = FontAttributes.Bold,
                FontSize = 16,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                LineBreakMode = LineBreakMode.TailTruncation
            };

            _placeholder = new Border
            {
                Stroke = Theme.TextMuted,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 3 },
                StrokeShape = new RoundRectangle { CornerRadius = 10 },
                BackgroundColor = Color.FromRgba(255, 255, 255, 0.10),
                HeightRequest = 80,
                Margin = new Thickness(0, 5, 0, 10),
                Padding = new Thickness(12, 0),
                Content = _placeholderLabel
            };

            // Dropping on the slot itself also works
            var placeholderDrop = new DropGestureRecognizer { AllowDrop = true };
            placeholderDrop.DragOver += (s, e) =>
            {
                if (_dragged != null)
                {
                    e.AcceptedOperation = DataPackageOperation.Copy;
                }
            };
            placeholderDrop.Drop += (s, e) => DropAtPlaceholder();
            _placeholder.GestureRecognizers.Add(placeholderDrop);
        }

        // title gives the exercise name shown in the drop slot
        public void Attach(Frame card, View handle, Func<string> title)
        {
            var drag = new DragGestureRecognizer();
            drag.DragStarting += (s, e) =>
            {
                _dragged = card;
                e.Data.Text = "exercise";
                _placeholderLabel.Text = $"Move {title()} here";
                card.Opacity = 0.35; // The "lifted" card stays in place, faded
                _setCompact?.Invoke(true);
            };
            drag.DropCompleted += (s, e) => EndDrag();
            handle.GestureRecognizers.Add(drag);

            var drop = new DropGestureRecognizer { AllowDrop = true };
            drop.DragOver += (s, e) =>
            {
                if (_dragged == null)
                {
                    return;
                }

                e.AcceptedOperation = DataPackageOperation.Copy;

                var position = e.GetPosition(card);
                bool below = position.HasValue && position.Value.Y > card.Height / 2;
                ShowPlaceholder(card, below);
            };
            drop.Drop += (s, e) => DropAtPlaceholder();
            card.GestureRecognizers.Add(drop);
        }

        // Puts the gray slot above or below the target card. Hides it when dropping there
        // wouldn't move anything (on the card itself or right next to it).
        private void ShowPlaceholder(Frame target, bool below)
        {
            var cards = _container.Children.Where(c => c != _placeholder).ToList();
            int targetIndex = cards.IndexOf(target);
            int draggedIndex = cards.IndexOf(_dragged);
            if (targetIndex < 0 || draggedIndex < 0)
            {
                return;
            }

            int insertIndex = targetIndex + (below ? 1 : 0);
            bool noMove = target == _dragged || insertIndex == draggedIndex || insertIndex == draggedIndex + 1;

            int currentIndex = _container.Children.IndexOf(_placeholder);
            if (noMove)
            {
                if (currentIndex >= 0)
                {
                    _container.Children.Remove(_placeholder);
                }
                return;
            }

            // insertIndex counts cards only; convert to a position in the container
            View anchor = insertIndex < cards.Count ? (View)cards[insertIndex] : null;
            int wanted = anchor != null ? _container.Children.IndexOf(anchor) : _container.Children.Count;
            if (currentIndex >= 0 && currentIndex < wanted)
            {
                wanted--; // Removing the slot first shifts later positions up by one
            }

            if (currentIndex == wanted)
            {
                return; // Already there; moving it again would make the list jump
            }

            if (currentIndex >= 0)
            {
                _container.Children.Remove(_placeholder);
            }
            _container.Children.Insert(wanted, _placeholder);
        }

        private void DropAtPlaceholder()
        {
            var dragged = _dragged;
            int slot = _container.Children.IndexOf(_placeholder);

            if (dragged != null && slot >= 0)
            {
                _container.Children.Remove(_placeholder);
                int from = _container.Children.IndexOf(dragged);
                _container.Children.Remove(dragged);
                if (from < slot)
                {
                    slot--;
                }
                _container.Children.Insert(slot, dragged);
                _onReordered?.Invoke();
            }

            EndDrag();
        }

        private void EndDrag()
        {
            if (_container.Children.Contains(_placeholder))
            {
                _container.Children.Remove(_placeholder);
            }

            if (_dragged == null)
            {
                return;
            }

            _dragged.Opacity = 1;
            _dragged = null;
            _setCompact?.Invoke(false);
        }

        // The "≡" grip shown on each card
        public static Label CreateHandle()
        {
            return new Label
            {
                Text = "≡",
                FontSize = 28,
                TextColor = Theme.TextMuted,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Start,
                Padding = new Thickness(4, 0, 12, 0)
            };
        }
    }
}
