import 'package:emoji_picker_flutter/emoji_picker_flutter.dart';
import 'package:flutter/material.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/controllers/local_controllers/local_patient_controller.dart';
import 'package:frontend/controllers/patient_controller.dart';
import 'package:frontend/models/patient.dart';
import 'package:frontend/widgets/helper_widgets/emojis/custom_emoji_category.dart';

// Custom view of the entire EmojiPicker widget, will encase category view, emojiview and bottombar view
class CustomView extends EmojiPickerView {
  // the controller that contains the emoji
  final TextEditingController controller;
  // the patient associated with the emoji picker
  final Patient patient;

  const CustomView(super.config, super.state, super.showSearchBar,
      this.controller, this.patient,
      {super.key});

  @override
  _CustomViewState createState() => _CustomViewState();
}

class _CustomViewState extends State<CustomView>
    with SingleTickerProviderStateMixin, SkinToneOverlayStateMixin {
  // control the movmeent of the tabs
  late TabController _tabController;
  // control the pages that are assoicated with the tabs.
  // contains the emojis that are for the intended tab (food category for food page)
  late PageController _pageController;
  // scroll controller for scrolling through the emoji view (middle)
  final _scrollController = ScrollController();

  // the list of specified categories
  final List<CategoryEmoji> categoryEmojis =
      defaultEmojiSet.where((x) => x.category == Category.FOODS).toList();

  // initalize the widget on startup
  @override
  void initState() {
    // grab the default initial category to show up when first clicked
    var initCategory = categoryEmojis.indexWhere((element) =>
        element.category == widget.config.categoryViewConfig.initCategory);
    // slight error check
    if (initCategory == -1) {
      initCategory = 0;
    }
    // initalize tab controller with the intended categories
    _tabController = TabController(
        initialIndex: initCategory, length: categoryEmojis.length, vsync: this);
    // assoicate the pages with the tabs
    _pageController = PageController(initialPage: initCategory)
      ..addListener(closeSkinToneOverlay);
    // add the scroll controller
    _scrollController.addListener(closeSkinToneOverlay);
    super.initState();
  }

  // when widget is no longer in use, dispose
  @override
  void dispose() {
    closeSkinToneOverlay();
    _pageController.dispose();
    _scrollController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        // specify the size of eomji based on screen size
        final emojiSize =
            widget.config.emojiViewConfig.getEmojiSize(constraints.maxWidth);
        final emojiBoxSize =
            widget.config.emojiViewConfig.getEmojiBoxSize(constraints.maxWidth);
        // assoicate the emojis with the specified config at declaration of emojiPicker (config)
        return EmojiContainer(
          color: widget.config.emojiViewConfig.backgroundColor,
          buttonMode: widget.config.emojiViewConfig.buttonMode,
          child: Column(
            // the order of which emojiPicker will show in
            children: [
              widget.config.viewOrderConfig.top, // categoryView
              widget.config.viewOrderConfig.middle, // emojiView
              widget.config.viewOrderConfig.bottom, // searchBar
            ].map(
              (item) {
                // will build for each item (category) the emojiView for it
                switch (item) {
                  case EmojiPickerItem.categoryBar:
                    // Category view
                    return _buildCategoryView();
                  case EmojiPickerItem.emojiView:
                    // Emoji view
                    return _buildEmojiView(emojiSize, emojiBoxSize);
                  case EmojiPickerItem.searchBar:
                    // Search Bar
                    return _buildBottomSearchBar();
                }
              },
            ).toList(),
          ),
        );
      },
    );
  }

  // will build each category, passing it to our custom
  // category view
  Widget _buildCategoryView() {
    return CustomEmojiCategory(
      widget.config,
      widget.state,
      _tabController,
      _pageController,
      categoryEmojis,
    );
  }

  // will build emoji view that has all the emojis
  // takes in the size of the emoji and how many emojois each row will contain
  Widget _buildEmojiView(double emojiSize, double emojiBoxSize) {
    return Flexible(
      child: PageView.builder(
        itemCount: categoryEmojis.length,
        controller: _pageController,
        // will change the page to the specified category
        onPageChanged: (index) {
          _tabController.animateTo(
            index,
            duration: widget.config.categoryViewConfig.tabIndicatorAnimDuration,
          );
        },
        // load each emoji inside the emojiView
        itemBuilder: (context, index) => _buildPage(
          emojiSize,
          emojiBoxSize,
          categoryEmojis[index],
        ),
      ),
    );
  }

  // build the bottom search bar
  Widget _buildBottomSearchBar() {
    if (!widget.config.bottomActionBarConfig.enabled) {
      return const SizedBox.shrink();
    }
    return DefaultBottomActionBar(
      widget.config,
      widget.state,
      widget.showSearchBar,
    );
  }

  // will load each emoji inside the emojiView
  Widget _buildPage(
      double emojiSize, double emojiBoxSize, CategoryEmoji categoryEmoji) {
    // Display notice if recent has no entries yet
    if (categoryEmoji.category == Category.RECENT &&
        categoryEmoji.emoji.isEmpty) {
      return _buildNoRecent();
    }
    // Build page normally
    return GridView.builder(
      // load each emoji inside a grid while allowing vertical scrolling
      key: const Key('emojiScrollView'),
      scrollDirection: Axis.vertical,
      controller: _scrollController,
      primary: false,
      padding: widget.config.emojiViewConfig.gridPadding,
      gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
        childAspectRatio: 1,
        crossAxisCount: widget.config.emojiViewConfig.columns,
        mainAxisSpacing: widget.config.emojiViewConfig.verticalSpacing,
        crossAxisSpacing: widget.config.emojiViewConfig.horizontalSpacing,
      ),
      itemCount: categoryEmoji.emoji.length,
      itemBuilder: (context, index) {
        return addSkinToneTargetIfAvailable(
          hasSkinTone: categoryEmoji.emoji[index].hasSkinTone,
          linkKey:
              categoryEmoji.category.name + categoryEmoji.emoji[index].emoji,
          child: EmojiCell.fromConfig(
            emoji: categoryEmoji.emoji[index],
            emojiSize: emojiSize,
            emojiBoxSize: emojiBoxSize,
            categoryEmoji: categoryEmoji,
            onEmojiSelected: _onSkinTonedEmojiSelected,
            onSkinToneDialogRequested: _openSkinToneDialog,
            config: widget.config,
          ),
        );
      },
    );
  }

  /// Build Widget for when no recent emoji are available
  Widget _buildNoRecent() {
    return Center(
      child: widget.config.emojiViewConfig.noRecents,
    );
  }

  // Allowing for the skin selection of skin-colored emojis
  void _openSkinToneDialog(
    Offset emojiBoxPosition,
    Emoji emoji,
    double emojiSize,
    CategoryEmoji? categoryEmoji,
  ) {
    closeSkinToneOverlay();
    if (!emoji.hasSkinTone || !widget.config.skinToneConfig.enabled) {
      return;
    }
    showSkinToneOverlay(
      emojiBoxPosition,
      emoji,
      emojiSize,
      categoryEmoji,
      widget.config,
      _onSkinTonedEmojiSelected,
      links[categoryEmoji!.category.name + emoji.emoji]!,
    );
  }

  // the onlclick of the individual emoji inside the emoji view
  void _onSkinTonedEmojiSelected(Category? category, Emoji emoji) async {
    // clear the controller to only allow 1 emoji in text controlller
    widget.controller.clear();
    widget.state.onEmojiSelected(category, emoji);
    closeSkinToneOverlay();
    // set the patient's emoji
    widget.patient.emoji = emoji.emoji;
    // send it off the the backend
    // If in guest use local storage
    if (AuthController().isGuestLoggedIn()) {
      widget.patient.therapistId = AuthController().guestId;
      await LocalPatientController.updatePatient(widget.patient);
    }
    else {
      await PatientController.modifyPatientByID(widget.patient);
    }
  }
}
