namespace FitnessApp
{
    // Drag-to-reorder for exercise cards in a StackLayout. Hold a card's handle, drag it onto
    // another card, and it lands above or below that card depending on where you let go.
    // While dragging, the page can collapse its cards (setCompact) so every exercise fits on screen.
    public class CardReorder
    {
        private readonly Layout _container;
        private readonly Action<bool> _setCompact;
        private readonly Action _onReordered;
        private Frame _dragged;
        private Frame _highlighted;

        public CardReorder(Layout container, Action<bool> setCompact, Action onReordered = null)
        {
            _container = container;
            _setCompact = setCompact;
            _onReordered = onReordered;
        }

        public void Attach(Frame card, View handle)
        {
            var drag = new DragGestureRecognizer();
            drag.DragStarting += (s, e) =>
            {
                _dragged = card;
                e.Data.Text = "exercise";
                _setCompact?.Invoke(true);
            };
            drag.DropCompleted += (s, e) => EndDrag();
            handle.GestureRecognizers.Add(drag);

            var drop = new DropGestureRecognizer { AllowDrop = true };
            drop.DragOver += (s, e) =>
            {
                if (_dragged == null || _dragged == card)
                {
                    return;
                }

                e.AcceptedOperation = DataPackageOperation.Copy;
                Highlight(card);
            };
            drop.DragLeave += (s, e) =>
            {
                if (_highlighted == card)
                {
                    Highlight(null);
                }
            };
            drop.Drop += (s, e) =>
            {
                var dragged = _dragged;
                if (dragged != null && dragged != card && _container.Children.Contains(card))
                {
                    var position = e.GetPosition(card);
                    bool below = position.HasValue && position.Value.Y > card.Height / 2;

                    _container.Children.Remove(dragged);
                    int index = _container.Children.IndexOf(card) + (below ? 1 : 0);
                    _container.Children.Insert(index, dragged);
                    _onReordered?.Invoke();
                }

                EndDrag();
            };
            card.GestureRecognizers.Add(drop);
        }

        private void Highlight(Frame card)
        {
            if (_highlighted != null)
            {
                _highlighted.BorderColor = Theme.Border;
            }

            _highlighted = card;

            if (_highlighted != null)
            {
                _highlighted.BorderColor = Theme.Accent;
            }
        }

        private void EndDrag()
        {
            if (_dragged == null && _highlighted == null)
            {
                return;
            }

            _dragged = null;
            Highlight(null);
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
