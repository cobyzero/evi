export abstract class Widget {
  // Base widget class
}

export abstract class StatelessWidget extends Widget {
  abstract build(): Widget;
}
