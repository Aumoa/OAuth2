export const coreMessages = {
  ko: {
    core: {
      dialog: {
        ariaLabel: '대화 상자',
        closeLabel: '대화 상자 닫기',
      },
      languageSelector: {
        changeLabel: '언어 변경',
        dialogTitle: '언어 선택',
        closeLabel: '언어 선택 닫기',
        description: '화면에 표시할 언어를 선택하세요.',
        availableLanguages: '사용 가능한 언어',
        searchPlaceholder: '언어 검색',
        clearSearch: '검색어 지우기',
        noResults: '검색 결과가 없습니다.',
        recommended: '추천',
        allLanguages: '모든 언어',
        searchResults: '검색 결과',
        systemDefault: '시스템 기본 언어',
        languageNames: {
          ko: '한국어',
          en: '영어',
          ja: '일본어',
        },
      },
      themeSelector: {
        useDark: '어두운 테마 사용',
        useLight: '밝은 테마 사용',
      },
    },
  },
  en: {
    core: {
      dialog: {
        ariaLabel: 'Dialog',
        closeLabel: 'Close dialog',
      },
      languageSelector: {
        changeLabel: 'Change language',
        dialogTitle: 'Select language',
        closeLabel: 'Close language selector',
        description: 'Choose the language to display on the screen.',
        availableLanguages: 'Available languages',
        searchPlaceholder: 'Search languages',
        clearSearch: 'Clear search',
        noResults: 'No languages found.',
        recommended: 'Recommended',
        allLanguages: 'All languages',
        searchResults: 'Search results',
        systemDefault: 'System language',
        languageNames: {
          ko: 'Korean',
          en: 'English',
          ja: 'Japanese',
        },
      },
      themeSelector: {
        useDark: 'Use dark theme',
        useLight: 'Use light theme',
      },
    },
  },
  ja: {
    core: {
      dialog: {
        ariaLabel: 'ダイアログ',
        closeLabel: 'ダイアログを閉じる',
      },
      languageSelector: {
        changeLabel: '言語を変更',
        dialogTitle: '言語を選択',
        closeLabel: '言語選択を閉じる',
        description: '画面に表示する言語を選択してください。',
        availableLanguages: '利用可能な言語',
        searchPlaceholder: '言語を検索',
        clearSearch: '検索をクリア',
        noResults: '言語が見つかりません。',
        recommended: 'おすすめ',
        allLanguages: 'すべての言語',
        searchResults: '検索結果',
        systemDefault: 'システムの言語',
        languageNames: {
          ko: '韓国語',
          en: '英語',
          ja: '日本語',
        },
      },
      themeSelector: {
        useDark: 'ダークテーマを使用',
        useLight: 'ライトテーマを使用',
      },
    },
  },
} as const;
