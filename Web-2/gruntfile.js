module.exports = function(grunt) {
  grunt.loadNpmTasks('grunt-markdown')
  grunt.loadNpmTasks('grunt-contrib-less')
  grunt.loadNpmTasks('grunt-contrib-cssmin')
  grunt.loadNpmTasks('grunt-contrib-copy')
  grunt.loadNpmTasks('grunt-devserver')
  grunt.loadNpmTasks('grunt-contrib-watch')

  grunt.initConfig({
    markdown: {
      all: {
        files: [
          {
            expand: true,
            flatten: true,
            src: 'src/*.md',
            dest: 'dist/.',
            ext: '.html'
          },
          {
            expand: true,
            flatten: true,
            src: 'src/*.html',
            dest: 'dist/.',
            ext: '.html'
          }
        ],
        options: {
          template: 'src/layout.jst'
        }
      }
    },
    less: {
      all: {
        files: {
          'dist/css/site.css': 'src/css/site.less'
        }
      }
    },
    cssmin: {
      target: {
        files: {
          'dist/css/site.min.css': 'dist/css/site.css'
        }
      }
    },
    copy: {
      all: {
        files: [
          {
            expand: true,
            cwd: 'node_modules',
            src: ['bootstrap/dist/**', 'jquery/dist/**'],
            dest: 'dist/lib/'
          },
          {
            expand: true,
            cwd: 'src',
            src: ['favicon.ico', 'images/**'],
            dest: 'dist'
          }
        ]
      }
    },
    watch: {
      scripts: {
        files: ['src/**/*.*'],
        tasks: ['gen'],
        options: {
          spawn: false
        }
      }
    },
    devserver: {
      dist: {
        options: {
          base: 'dist'
        }
      }
    }
  })

  grunt.registerTask('gen', ['markdown:all', 'less:all', 'cssmin', 'copy'])
}
