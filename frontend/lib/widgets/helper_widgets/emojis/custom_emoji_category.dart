import 'package:emoji_picker_flutter/emoji_picker_flutter.dart';
import 'package:flutter/material.dart';

// Represents the emoji category tab view at the top of the page
class CustomEmojiCategory extends CategoryView {
  // The list of emojis to display in the tab.
  final List<CategoryEmoji> categoryEmojis;

  const CustomEmojiCategory(super.config, super.state, super.tabController,
      super.pageController, this.categoryEmojis,
      {super.key});

  @override
  _CustomEmojiCategory createState() => _CustomEmojiCategory();
}

class _CustomEmojiCategory extends State<CustomEmojiCategory> {
  // Basically set all of the colors based on the config provided by CategoryConfig when
  // specifying EmojiPicker
  @override
  Widget build(final BuildContext context) {
    return Container(
        color: widget.config.categoryViewConfig.backgroundColor,
        // row of the category emojis
        child: Row(children: [
          Expanded(
            // each category icon specified in a sized box with config
            child: SizedBox(
              height: widget.config.categoryViewConfig.tabBarHeight,
              child: TabBar(
                labelColor: widget.config.categoryViewConfig.iconColorSelected,
                indicatorColor: widget.config.categoryViewConfig.indicatorColor,
                unselectedLabelColor:
                    widget.config.categoryViewConfig.iconColor,
                dividerColor: widget.config.categoryViewConfig.dividerColor,
                controller: widget.tabController,
                labelPadding: EdgeInsets.zero,
                // will go through pageController to transfer to the emojiView with the specified filter
                onTap: (index) {
                  widget.pageController.jumpToPage(index);
                },
                // finally map the tabs with a dispplay
                tabs: widget.categoryEmojis
                    .asMap()
                    .entries
                    .map<Widget>((item) =>
                        _buildCategoryTab(item.key, item.value.category))
                    .toList(),
              ),
            ),
          ),
        ]));
  }

  // will go through each category and grab the icons for them
  Widget _buildCategoryTab(int index, Category category) {
    return Tab(
      icon: Icon(
        getIconForCategory(
            widget.config.categoryViewConfig.categoryIcons, category),
      ),
    );
  }
}
