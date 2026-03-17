#pragma once

#include <chrono>
#include <filesystem>
#include <string>

class HotReloader {
public:
  explicit HotReloader(std::string distDir = "dist");

  bool shouldReload();

private:
  std::filesystem::file_time_type latestJsWriteTime() const;

  std::string distDir_;
  std::chrono::steady_clock::time_point lastCheck_;
  std::filesystem::file_time_type lastKnownWrite_;
  std::filesystem::file_time_type pendingWrite_;
  std::chrono::steady_clock::time_point pendingSince_;

  static constexpr std::chrono::milliseconds kPollInterval{250};
  static constexpr std::chrono::milliseconds kDebounce{500};
};
