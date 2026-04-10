import { useEffect, useState } from 'react';

export const ThemeToggle = () => {
  // 1. Initialize state based on the cookie
  const [isDark, setIsDark] = useState(() => {
    return document.cookie.includes('theme=dark');
  });

  useEffect(() => {
    if (isDark) {
      document.documentElement.classList.add('dark');
      // Set cookie to expire in 1 year
      document.cookie = "theme=dark; max-age=31536000; path=/; SameSite=Lax";
    } else {
      document.documentElement.classList.remove('dark');
      document.cookie = "theme=light; max-age=31536000; path=/; SameSite=Lax";
    }
  }, [isDark]);

  return (
    <button 
      onClick={() => setIsDark(!isDark)}
      className="p-2 rounded-md hover:bg-gray-200 dark:hover:bg-gray-700"
    >
      {isDark ? 'Light Mode' : 'Dark Mode'}
    </button>
  );
};