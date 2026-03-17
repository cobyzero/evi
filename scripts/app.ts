import { runApp, Column, Text, Container, StatelessWidget, Widget } from "../framework/widgets.js";

class MyCustomWidget extends StatelessWidget {
  build(): Widget {
    return Column([
      Text("Custom Widget!", "#000000"),
      Container({ width: 50, height: 250 , color: "#90e0ef" }),
    ]);
  }
}

runApp(
  Column([
    Text("Framework App", "#90e0ef"),
    new MyCustomWidget(),
    Text("Testing bindings...", "#f1faee")
  ]),
  {
    backgroundColor: "#ffffff",
    textColor: "#ffffff",
  }
);
