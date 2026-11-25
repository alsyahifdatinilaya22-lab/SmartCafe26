import 'package:flutter_test/flutter_test.dart';
import 'package:smart_cafe_mobile/main.dart';

void main() {
  testWidgets('SmartCafeApp smoke test', (WidgetTester tester) async {
    await tester.pumpWidget(const SmartCafeApp());
    expect(find.text('SmartCafe 26'), findsOneWidget);
  });
}