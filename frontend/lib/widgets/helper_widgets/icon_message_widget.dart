import 'package:flutter/material.dart';

/// A widget with an icon and a message displayed in gray
class IconMessageWidget extends StatelessWidget {
  final String message;
  final IconData? icon;

  const IconMessageWidget(
      {super.key,
      required this.message,
      this.icon = Icons.list_alt // default icon
      });

  @override
  Widget build(final BuildContext context) {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(icon, size: 100, color: Colors.grey),
          const SizedBox(height: 20),
          Text(
            message,
            style: const TextStyle(color: Colors.grey, fontSize: 18),
          ),
        ],
      ),
    );
  }
}
