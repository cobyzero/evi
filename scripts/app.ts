import { runApp, Column, Text, Container, StatelessWidget, Widget } from "../framework/widgets.js";

class MyCustomWidget extends StatelessWidget {
  build(): Widget {
    return Column([
      Text("Custom Widget!"),
      Container({ width: 50, height: 250 }),
    ]);
  }
}

runApp(
  Column([
    Text("Framework App"),
    new MyCustomWidget(),
    Text("Testing bindings...")
  ])
);
