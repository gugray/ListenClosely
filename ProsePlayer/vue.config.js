// vue.config.js
module.exports = {
  indexPath: 'player.html',
  //publicPath: process.env.NODE_ENV === 'production' ? '/prose/' : '/',
  publicPath: process.env.NODE_ENV === 'production' ? '/' : '/',
  //chainWebpack: config => {
  //  config.module.rules.delete('eslint');
  //},
}
