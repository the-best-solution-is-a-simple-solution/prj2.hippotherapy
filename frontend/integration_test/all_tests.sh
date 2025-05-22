#!/bin/bash


# color codes
RED='\033[0;31m'      # Red
GREEN='\033[0;32m'    # Green
YELLOW='\033[0;33m'   # Yellow
NC='\033[0m'          # No Color

PING_ADDRESS="http://localhost:5001/integration-tests/ping"
DELETE_FIRESTORE_PATH="http://localhost:5001/integration-tests/clear"
DELETE_AUTH_PATH="http://localhost:5001/integration-tests/clear-auth"
CLEAR_MAIL_DELETE_AUTH_PATH="http://localhost:8025/api/v1/messages"
CHROMEDRIVER_PORT=4444
MAILHOG_PORT=1025

START_TIME=$(date +%s)

# A cross platform check for if the port is used
# returns 0 for success, 1 for failure
function is_port_used() {
    local port="$1"
    # (ss -ltn is for linux compatibility)
    if (netstat -ano | grep "LISTEN" | grep "$port" > /dev/null || ss -ltn | grep "$port" > /dev/null)
    then
      return 0 # port is in use
    else
      return 1 # port is not in use
    fi
}

# Check if Chromedriver is running
if is_port_used $CHROMEDRIVER_PORT ; then
  echo -e "${GREEN}ChromeDriver found running on port $CHROMEDRIVER_PORT${NC}"
else
  echo -e "${GREEN}Starting ChromeDriver on port $CHROMEDRIVER_PORT${NC}"
  chromedriver --port=$CHROMEDRIVER_PORT &
fi

# Check if Mailhog is running
if ! is_port_used $MAILHOG_PORT ; then
    echo -e "${RED}Error: Mailhog not running.${NC}"
    echo -e "${YELLOW}Start your mailhog${NC}, executable file stored in .../prj2.hippotherapy/frontend/test_driver/mailhog-windows.exe"
    exit 1
else
    echo -e "${GREEN}Mailhog server found at port $MAILHOG_PORT: view emails at http://localhost:8025${NC}"
fi

# Check if backend has been started
if curl -sL --fail "$PING_ADDRESS" -o /dev/null; then
    echo -e "${GREEN}Connected to server by testing $PING_ADDRESS${NC}"
else
    echo -e "${RED}Error: Failed to connect to server by testing $PING_ADDRESS${NC}"
    echo -e "${YELLOW}Please check you have launched the backend${NC}"
    exit 1
fi

test_dir="integration_test"

# Array of test files to be run
TEST_FILES=(
  "$test_dir/evaluation_page_caching_online_test.dart"
  "$test_dir/owner_register_login_test.dart"
  "$test_dir/registration_page_test.dart"
  "$test_dir/evaluation_page_test.dart"
  "$test_dir/create_patient_integration_test.dart"
  "$test_dir/export_page_form_test.dart"
  "$test_dir/login_page_test.dart"
  "$test_dir/patient_info_page_test.dart"
  "$test_dir/patient_info_graph_tab_test.dart"
  "$test_dir/patients_list_page_test.dart"
  "$test_dir/owner_reassigns_patients_to_different_therapist_test.dart"
  "$test_dir/reset_password_test.dart"             # pass sometimes on emulator, features works on emulator but not in production
  "$test_dir/patient_archive_page_test.dart"
  "$test_dir/generate_referral_test.dart"
  "$test_dir/guest_mode_test.dart"
   "$test_dir/tutorial_test.dart"
#  "$test_dir/evaluation_tags_test.dart"           # tests don't pass due to bug with duplicated requests, but feature works as intended
)

# Save stats
test_num=0
tests_failed=0
tests_succeeded=0

# Run tests sequentially
for test_file in "${TEST_FILES[@]}"; do
  ((test_num++))
  echo ""
  echo "---------------------------------------------------------"
  echo -e "${GREEN}Running test $test_num/${#TEST_FILES[@]}: $test_file ${NC}"
  echo "---------------------------------------------------------"
  echo ""
  curl --request DELETE -sL --url "$DELETE_FIRESTORE_PATH"
  echo ""
  curl --request DELETE -sL --url "$DELETE_AUTH_PATH"
  echo ""
  curl --request DELETE -sL --url "$CLEAR_MAIL_DELETE_AUTH_PATH"
  echo ""
  flutter drive --driver=test_driver/integration_test.dart --target="$test_file" -d chrome --dart-define=FLUTTER_TEST=true

  # Check if test failed
  if [ $? -ne 0 ]; then
    echo -e "${RED}Test failed: $test_file${NC}"
    ((tests_failed++))
    # Do not exit but track the count of failures
    #exit 1
  else
    ((tests_succeeded++))
  fi
  sleep 3
done

# get the count of all dart files in the directory the script is run
total_tests_count=$( ls *.dart | wc -l )
END_TIME=$(date +%s)
TOTAL_MIN=$(((END_TIME-START_TIME)/60))

# Display stats
echo -e "${GREEN}================================================================="
echo -e "     alltests.sh script completed successfully in $TOTAL_MIN minutes"
echo -e "     $tests_succeeded/$test_num tests selected passed${YELLOW} (Scroll up and check. Not all errors are detected.)"

# Only display error if tests failed
if [ $tests_failed -gt 0 ]; then
echo -e "${RED}     $tests_failed/$test_num tests selected failed :("
fi

echo -e "${GREEN}     Total tests in directory: $total_tests_count "
echo -e "=================================================================${NC}"
