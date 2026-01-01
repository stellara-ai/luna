<template>
  <div class="app">
    <header>
      <h1>Luna — AI Teacher</h1>
    </header>
    <main>
      <p>Welcome to Luna, an adaptive AI teaching platform.</p>
      <router-view />
    </main>
  </div>
</template>

<script setup lang="ts">
</script>

<style scoped>
.app {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
  line-height: 1.5;
}

header {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  padding: 2rem;
  text-align: center;
}

header h1 {
  margin: 0;
}

main {
  max-width: 1200px;
  margin: 2rem auto;
  padding: 0 1rem;
}
</style>
