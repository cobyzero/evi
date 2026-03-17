import { runApp, Column, Text, Container, StatelessWidget, Widget, Row } from "../framework/widgets.js";

class MyCustomWidget extends StatelessWidget {
  build(): Widget {
    return Column([
      Text("Hello, World!", "#000000"),
      Container({ width: 250, height: 150 , color: "#90e0ef" }),
    ]);
  }
}

runApp(
  Column([
    Text("Evi Framework UI", "#5d0fe6"),
    new MyCustomWidget(),
    Text("Hi", "#47b123"),
    Row([
        Text("Hi", "#47b123"),
          Text("row", "#47b123"),
          Text("row", "#47b123"),
    ])
  ]),
  {
    backgroundColor: "#ffffff",
    textColor: "#ffffff",
  }
);
